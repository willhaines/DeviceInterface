﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECore
{
    abstract public class EDeviceHWInterface
    {
        abstract public void WriteControlBytes(byte[] message);
        abstract public byte[] ReadControlBytes(int length);
        abstract public byte[] GetData(int numberOfBytes);
        abstract public bool Connected { get; }
        abstract public void StartInterface();
        abstract public void StopInterface();
    }
}
