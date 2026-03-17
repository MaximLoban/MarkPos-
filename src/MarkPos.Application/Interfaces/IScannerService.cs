using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkPos.Application.Scanner;

namespace MarkPos.Application.Interfaces;

public interface IScannerService
{
    event Action<ScannerMessage> MessageReceived;
}