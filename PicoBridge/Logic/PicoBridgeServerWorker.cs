using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PicoBridge.Types;

namespace PicoBridge.Logic;

public class PicoBridgeServerWorker
{
    public delegate void ConnectEventHandler(object sender);

    public delegate void DatagramEventHandler(object sender, PicoFaceTrackingDatagram datagram);

    public delegate void DisconnectEventHandler(object sender);

    private const int PicoDatagramSize = 892;

    private readonly CancellationTokenSource cts;
    private readonly int port;
    private readonly Semaphore sem = new(1, 1);

    private readonly Thread thread;
    private DateTime connectionEstablishTime = DateTime.MinValue;
    private DateTime lastActivityTime = DateTime.MinValue;

    internal PicoBridgeServerWorker(int port)
    {
        cts = new CancellationTokenSource();
        this.port = port;
        thread = new Thread(() =>
        {
            try
            {
                Work(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Start()
    {
        thread.Start();
    }

    public void Stop()
    {
        cts.Cancel();
        thread.Join();
    }

    public void Join()
    {
        thread.Join();
    }

    public event ConnectEventHandler? Connect;

    public event DisconnectEventHandler? Disconnect;

    public event DatagramEventHandler? Data;

    private void OnDatagram(IAsyncResult x)
    {
        var state = (ServerWorkerState) x.AsyncState!;
        var client = state.Client;
        var endpoint = state.EndPoint;
        var data = client.EndReceive(x, ref endpoint);
        if (data.Length >= PicoDatagramSize)
        {
            lastActivityTime = DateTime.Now;
            if (connectionEstablishTime == DateTime.MinValue)
            {
                connectionEstablishTime = DateTime.Now;
                Connect?.Invoke(this);
            }

            var datagram = new PicoFaceTrackingDatagram(new MemoryStream(data));
            Data?.Invoke(this, datagram);
        }

        sem.Release();
    }

    private void Work(CancellationToken token)
    {
        var listener = new UdpClient(port);
        var endpoint = new IPEndPoint(IPAddress.Any, port);
        var state = new ServerWorkerState()
        {
            EndPoint = endpoint,
            Client = listener
        };

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (sem.WaitOne(500))
                {
                    listener.BeginReceive(OnDatagram, state);
                }

                if (lastActivityTime == DateTime.MinValue
                    || DateTime.Now - lastActivityTime <= TimeSpan.FromSeconds(5)) continue;

                // timeout, reset state
                lastActivityTime = DateTime.MinValue;
                connectionEstablishTime = DateTime.MinValue;
                Disconnect?.Invoke(this);
            }
        }
        finally
        {
            listener.Close();
        }
    }
}
