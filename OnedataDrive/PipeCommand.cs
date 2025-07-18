﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public class PipeCommand
    {
        public const string SEPARATOR = "|";
        public Commands command;
        public List<string> payload;
        public PipeCommand(Commands command, List<string>? payload = null)
        {
            this.command = command;
            this.payload = payload ?? new();
        }

        public PipeCommand(string msg)
        {
            this.payload = new();
            string[] rawContent = msg.Split(SEPARATOR);
            try
            {
                this.command = (Commands)Enum.Parse(typeof(Commands), rawContent[0]);
            }
            catch
            {
                this.command = Commands.UNKNOWN_COMMAND;
                return;
            }
            this.payload.AddRange(rawContent.Skip(1));
        }

        public override string ToString()
        {
            string msg = command.ToString();
            if (payload.Count != 0)
            {
                msg += SEPARATOR + string.Join(SEPARATOR, payload);
            }
            return msg;
        }

    }

    public enum Commands
    {
        UNKNOWN_COMMAND,
        OK,
        FAIL,
        SEND_ROOT,
        REFRESH_SPACE,
        REFRESH_FOLDER,
        REFRESH_FOLDER_DOWN
    }
}
