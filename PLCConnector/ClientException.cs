using System;
using System.Collections.Generic;
using System.Text;

namespace PLCConnector
{
    public class ClientException : Exception
    {

        public ClientException(string message) : base(message)
        {

        }

    }
}
