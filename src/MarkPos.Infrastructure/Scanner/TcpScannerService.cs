using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.Interfaces;
using MarkPos.Application.Scanner;

namespace MarkPos.Infrastructure.Scanner;

public sealed class TcpScannerService : IScannerService
{
    private readonly TcpScannerListener _listener;

    public TcpScannerService(TcpScannerListener listener)
        => _listener = listener;

    public event Action<ScannerMessage>? MessageReceived
    {
        add => _listener.MessageReceived += value;
        remove => _listener.MessageReceived -= value;
    }
}