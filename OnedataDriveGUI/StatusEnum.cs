﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDriveGUI
{
    public enum Status : uint
    {
        NOT_CONNECTED,
        CONNECTED,
        ERROR,
        CONNECTING,
        DISCONNECTING
    }
}
