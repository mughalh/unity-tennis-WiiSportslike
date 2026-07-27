using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class Vector3Data
{
    public float x, y, z;
}

public class QuaternionData
{
    public float x, y, z, w;
}

public class IMUData
{
    public ulong timestamp;
    public Vector3Data accel;
    public Vector3Data gyro;
    public QuaternionData orientation;
    public string frame_id;
}

public static class ProtobufParser
{
    public static ulong ReadVarint(byte[] data, ref int offset)
    {
        ulong result = 0;
        int shift = 0;
        while (offset < data.Length)
        {
            byte b = data[offset++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }
        return result;
    }

    public static float ReadFloat(byte[] data, ref int offset)
    {
        float val = BitConverter.ToSingle(data, offset);
        offset += 4;
        return val;
    }

    public static byte[] ReadBytes(byte[] data, ref int offset)
    {
        ulong len = ReadVarint(data, ref offset);
        byte[] result = new byte[len];
        Array.Copy(data, offset, result, 0, (int)len);
        offset += (int)len;
        return result;
    }

    public static Vector3Data ParseVector3(byte[] data, ref int offset)
    {
        Vector3Data v = new Vector3Data();
        int length = (int)ReadVarint(data, ref offset);
        int end = offset + length;
        while (offset < end)
        {
            ulong tag = ReadVarint(data, ref offset);
            int field = (int)(tag >> 3);
            int wire = (int)(tag & 0x07);
            if (wire == 5 && field >= 1 && field <= 3)
            {
                float val = ReadFloat(data, ref offset);
                if (field == 1) v.x = val;
                else if (field == 2) v.y = val;
                else if (field == 3) v.z = val;
            }
            else
            {
                if (wire == 0) ReadVarint(data, ref offset);
                else if (wire == 2) { ulong len = ReadVarint(data, ref offset); offset += (int)len; }
                else if (wire == 1) offset += 8;
                else if (wire == 5) offset += 4;
            }
        }
        return v;
    }

    public static QuaternionData ParseQuaternion(byte[] data, ref int offset)
    {
        QuaternionData q = new QuaternionData();
        int length = (int)ReadVarint(data, ref offset);
        int end = offset + length;
        while (offset < end)
        {
            ulong tag = ReadVarint(data, ref offset);
            int field = (int)(tag >> 3);
            int wire = (int)(tag & 0x07);
            if (wire == 5 && field >= 1 && field <= 4)
            {
                float val = ReadFloat(data, ref offset);
                if (field == 1) q.x = val;
                else if (field == 2) q.y = val;
                else if (field == 3) q.z = val;
                else if (field == 4) q.w = val;
            }
            else
            {
                if (wire == 0) ReadVarint(data, ref offset);
                else if (wire == 2) { ulong len = ReadVarint(data, ref offset); offset += (int)len; }
                else if (wire == 1) offset += 8;
                else if (wire == 5) offset += 4;
            }
        }
        return q;
    }

    public static IMUData ParseIMUData(byte[] data, ref int offset)
    {
        IMUData imu = new IMUData();
        int length = (int)ReadVarint(data, ref offset);
        int end = offset + length;
        while (offset < end)
        {
            ulong tag = ReadVarint(data, ref offset);
            int field = (int)(tag >> 3);
            int wire = (int)(tag & 0x07);

            if (field == 1 && wire == 0)
            {
                imu.timestamp = ReadVarint(data, ref offset);
            }
            else if (field == 2 && wire == 2)
            {
                imu.accel = ParseVector3(data, ref offset);
            }
            else if (field == 3 && wire == 2)
            {
                imu.gyro = ParseVector3(data, ref offset);
            }
            else if (field == 4 && wire == 2)
            {
                imu.orientation = ParseQuaternion(data, ref offset);
            }
            else if (field == 5 && wire == 2)
            {
                byte[] strBytes = ReadBytes(data, ref offset);
                imu.frame_id = Encoding.UTF8.GetString(strBytes);
            }
            else
            {
                if (wire == 0) ReadVarint(data, ref offset);
                else if (wire == 2) { ulong len = ReadVarint(data, ref offset); offset += (int)len; }
                else if (wire == 1) offset += 8;
                else if (wire == 5) offset += 4;
            }
        }
        return imu;
    }

    public static IMUData ParseSensorMessage(byte[] data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            ulong tag = ReadVarint(data, ref offset);
            int field = (int)(tag >> 3);
            int wire = (int)(tag & 0x07);
            if (field == 1 && wire == 2)
            {
                return ParseIMUData(data, ref offset);
            }
            else
            {
                if (wire == 0) ReadVarint(data, ref offset);
                else if (wire == 2) { ulong len = ReadVarint(data, ref offset); offset += (int)len; }
                else if (wire == 1) offset += 8;
                else if (wire == 5) offset += 4;
            }
        }
        return null;
    }
}

public class SensorNetworkReceiver : MonoBehaviour
{
    [Header("Network")]
    public int tcpPort = 5678;

    [Header("Debug")]
    public bool showConnectionStatus = true;

    public event Action<IMUData> OnIMUDataReceived;

    private TcpListener tcpListener;
    private Thread listenerThread;
    private bool isRunning = false;
    private TcpClient currentClient;
    private NetworkStream stream;
    private bool isConnected = false;

    private Dictionary<uint, Dictionary<uint, byte[]>> chunks = new Dictionary<uint, Dictionary<uint, byte[]>>();

    private IMUData latestIMU;
    private bool hasData = false;
    private int dataCount = 0;

    private Vector3 latestAccel;
    private Vector3 latestGyro;
    private Quaternion latestOrient;
    private string localIP;

    void Start()
    {
        isRunning = true;
        localIP = GetLocalIPAddress();
        StartTCPServer();
    }

    void StartTCPServer()
    {
        try
        {
            listenerThread = new Thread(new ThreadStart(ServerThread));
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
        catch (Exception) { }
    }

    string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    void ServerThread()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            tcpListener.Start();

            while (isRunning)
            {
                try
                {
                    if (tcpListener.Pending())
                    {
                        currentClient = tcpListener.AcceptTcpClient();
                        isConnected = true;
                        stream = currentClient.GetStream();
                        HandleClient();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }
    }

    void HandleClient()
    {
        byte[] buffer = new byte[65536];
        byte[] dataBuffer = new byte[0];

        while (isRunning && currentClient != null && currentClient.Connected)
        {
            try
            {
                if (stream != null && stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        byte[] newBuffer = new byte[dataBuffer.Length + bytesRead];
                        Buffer.BlockCopy(dataBuffer, 0, newBuffer, 0, dataBuffer.Length);
                        Buffer.BlockCopy(buffer, 0, newBuffer, dataBuffer.Length, bytesRead);
                        dataBuffer = newBuffer;
                        ProcessBuffer(ref dataBuffer);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception)
            {
                break;
            }
        }

        isConnected = false;
        chunks.Clear();
    }

    void ProcessBuffer(ref byte[] dataBuffer)
    {
        const int HEADER_SIZE = 13;

        while (dataBuffer.Length >= HEADER_SIZE)
        {
            uint msgId = (uint)((dataBuffer[0] << 24) | (dataBuffer[1] << 16) | (dataBuffer[2] << 8) | dataBuffer[3]);
            uint seq = (uint)((dataBuffer[4] << 24) | (dataBuffer[5] << 16) | (dataBuffer[6] << 8) | dataBuffer[7]);
            bool isLast = dataBuffer[8] != 0;
            uint dataSize = (uint)((dataBuffer[9] << 24) | (dataBuffer[10] << 16) | (dataBuffer[11] << 8) | dataBuffer[12]);

            if (dataBuffer.Length < HEADER_SIZE + dataSize)
                break;

            byte[] chunkData = new byte[dataSize];
            Buffer.BlockCopy(dataBuffer, HEADER_SIZE, chunkData, 0, (int)dataSize);

            byte[] newBuffer = new byte[dataBuffer.Length - (HEADER_SIZE + dataSize)];
            Buffer.BlockCopy(dataBuffer, HEADER_SIZE + (int)dataSize, newBuffer, 0, newBuffer.Length);
            dataBuffer = newBuffer;

            if (!chunks.ContainsKey(msgId))
                chunks[msgId] = new Dictionary<uint, byte[]>();

            if (seq == 0)
                chunks[msgId].Clear();

            chunks[msgId][seq] = chunkData;

            if (isLast)
            {
                uint maxSeq = 0;
                foreach (var key in chunks[msgId].Keys)
                    if (key > maxSeq) maxSeq = key;

                bool complete = true;
                for (uint i = 0; i <= maxSeq; i++)
                    if (!chunks[msgId].ContainsKey(i)) { complete = false; break; }

                if (complete)
                {
                    List<byte> fullData = new List<byte>();
                    for (uint i = 0; i <= maxSeq; i++)
                        fullData.AddRange(chunks[msgId][i]);

                    IMUData imu = ProtobufParser.ParseSensorMessage(fullData.ToArray());
                    if (imu != null)
                    {
                        latestIMU = imu;
                        hasData = true;
                        dataCount++;

                        latestAccel = new Vector3(imu.accel.x, imu.accel.y, imu.accel.z);
                        latestGyro = new Vector3(imu.gyro.x, imu.gyro.y, imu.gyro.z);
                        latestOrient = new Quaternion(imu.orientation.x, imu.orientation.y, imu.orientation.z, imu.orientation.w);

                        OnIMUDataReceived?.Invoke(imu);
                    }

                    chunks.Remove(msgId);
                }
            }
        }
    }

    public Vector3 GetLatestAccel() => latestAccel;
    public Vector3 GetLatestGyro() => latestGyro;
    public Quaternion GetLatestOrientation() => latestOrient;
    public bool HasData() => hasData;
    public bool IsConnected() => isConnected;

    void OnGUI()
    {
        if (!showConnectionStatus) return;

        GUI.Box(new Rect(10, 10, 280, 80), "Sensor Connection");

        GUILayout.BeginArea(new Rect(20, 40, 260, 60));
        GUILayout.Label($"IP: {localIP}:{tcpPort}");
        GUILayout.Label($"Status: {(isConnected ? "Connected" : "Disconnected")}");
        GUILayout.EndArea();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (stream != null) { try { stream.Close(); } catch { } }
        if (currentClient != null) { try { currentClient.Close(); } catch { } }
        if (tcpListener != null) { try { tcpListener.Stop(); } catch { } }
        if (listenerThread != null && listenerThread.IsAlive)
        { try { listenerThread.Abort(); } catch { } }
    }
}