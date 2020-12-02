using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// Modbus TCP common driver class
/// </summary>
namespace AdamModbusDriver
{
    public class ModbusTCP
    {
        // constants for modbus functions
        private const byte _fctReadCoil = 1;
        private const byte _fctReadDiscreteInputs = 2;
        private const byte _fctReadHoldingRegister = 3;
        private const byte _fctReadInputRegister = 4;
        private const byte _fctWriteSingleCoil = 5;
        private const byte _fctWriteSingleRegister = 6;
        private const byte _fctWriteMultipleCoils = 15;
        private const byte _fctWriteMultipleRegistes = 16;
        private const byte _fctReadWriteMultipleRegister = 23;

        // Constants for exception return messages
        /// <summary>
        /// Illegal function exception constant
        /// </summary>
        public const byte excIllegalFunction = 1;
        /// <summary>
        /// Illegal data address exception constant
        /// </summary>
        public const byte excIllegalDataAdr = 2;
        /// <summary>
        /// Data value exception exception constant
        /// </summary>
        public const byte excIllegalDataValue = 3;
        /// <summary>
        /// Slave device failure exception constant
        /// </summary>
        public const byte excIllegalSlaveDeviceFailure = 4;
        /// <summary>
        /// Acknowledge exception constant
        /// </summary>
        public const byte excAck = 5;
        /// <summary>
        /// Slave is busy exception constant
        /// </summary>
        public const byte excSlaveIsBusy = 6;
        /// <summary>
        /// Gate path unavailable exception constant
        /// </summary>
        public const byte excGatePathUnavailable = 10;
        /// <summary>
        /// Not connected exception constant
        /// </summary>
        public const byte excNotConnected = 253;
        /// <summary>
        /// Connection lost exception constant
        /// </summary>
        public const byte excConnectionLost = 254;
        /// <summary>
        /// Connection timeout exception constant
        /// </summary>
        public const byte excConnectionTimeout = 255;
        /// <summary>
        /// Wrong offset exception constant
        /// </summary>
        public const byte excWrongOffset = 128;
        /// <summary>
        /// Send failed exception constant
        /// </summary>
        public const byte excSendFailed = 100;


        // Conection related  declarations
        /// <summary>
        /// Shows if a connection is active
        /// </summary>
        public bool Connected { get => _connected; }

        /// <summary>
        /// Display the connection state (sync or async)
        /// <value>Default value is True. Set to True if async connection required</value>
        /// </summary>
        public bool SyncConnection { get => _syncConnection; set => _syncConnection = value; }

        /// <summary>
        /// Refresh timer for slave answear
        /// <value>Default value is 10ms</value>
        /// </summary>
        public ushort Refresh { get => _refresh; set => _refresh = value; }

        /// <summary>
        /// Response timeout.
        /// <value>Default values is 500ms</value>
        /// </summary>
        public int Timeout { get => _timeout; set => _timeout = value; }


        // private declarations
        private int _timeout = 500;
        private ushort _refresh = 10;
        private bool _syncConnection = true;
        private bool _connected = false;

        // sockets from connection
        private Socket _tcpSyncronousSocket;
        private byte[] _tcpSyncronousBufferArray = new byte[2048];

        private Socket _tcpASyncronousSocket;
        private byte[] _tcpASyncronousBufferArray = new byte[2048];


        //Events for asyncronous connections
        /// <summary>
        /// Response data delegate
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unit"></param>
        /// <param name="function"></param>
        /// <param name="data"></param>
        public delegate void ResponseData(ushort id, byte unit, byte function, byte[] data);
        /// <summary>
        /// Response data event. This event is called when data arrives
        /// </summary>
        public event ResponseData OnResponseData;

        /// <summary>
        /// Exception data delagate
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unit"></param>
        /// <param name="function"></param>
        /// <param name="exception"></param>
        public delegate void ExceptionData(ushort id, byte unit, byte function, byte exception);
        /// <summary>
        /// Exception data event. The event is called when exception occurs
        /// </summary>
        public event ExceptionData OnException;

        /// <summary>
        /// Master Instance with no param specified.
        /// </summary>
        public ModbusTCP()
        {
        }

        /// <summary>
        /// Default instance witn syncronous connection
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public ModbusTCP(string ip, ushort port)
        {
            Connect(ip, port, true);
        }

        /// <summary>
        /// Instance with syncConnection param. 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="syncConnection">Set to false if async connection required</param>
        public ModbusTCP(string ip, ushort port, bool syncConnection)
        {
            Connect(ip, port, syncConnection);
        }

        /// <summary>
        /// Start connection to slave device
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="syncConnection">True if sync requiered/ False if async required</param>
        public void Connect(string ip, ushort port, bool syncConnection)
        {
            try
            {
                _syncConnection = syncConnection;

                //check if ip can be parsed and casted to IPAddress type
                if (IPAddress.TryParse(ip, out IPAddress _ip) == false)
                {
                    IPHostEntry iPHost = Dns.GetHostEntry(ip);
                    ip = iPHost.AddressList[0].ToString();
                }

                // connect syncronous client if true, else
                _tcpASyncronousSocket = new Socket(IPAddress.Parse(ip).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _tcpASyncronousSocket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                _tcpASyncronousSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout);
                _tcpASyncronousSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout);
                _tcpASyncronousSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                if (SyncConnection)
                {
                    _tcpSyncronousSocket = new Socket(IPAddress.Parse(ip).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _tcpSyncronousSocket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                    _tcpSyncronousSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout);
                    _tcpSyncronousSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout);
                    _tcpSyncronousSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                }


                _connected = true;

            }
            catch (Exception)
            {
                _connected = false;
                throw;
            }
        }

        public void Disconnect()
        {
            Dispose();
        }

        // clear up resources
        private void Dispose()
        {
            if (_tcpASyncronousSocket != null)
            {
                if (_tcpASyncronousSocket.Connected)
                {
                    try
                    {
                        _tcpASyncronousSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _tcpASyncronousSocket.Close();
                        _tcpASyncronousSocket = null;
                    }

                }
            }
            if (_tcpSyncronousSocket != null)
            {
                if (_tcpSyncronousSocket.Connected)
                {
                    try
                    {
                        _tcpSyncronousSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _tcpSyncronousSocket.Close();
                        _tcpSyncronousSocket = null;
                    }

                }
            }
        }

        private void CallException(ushort id, byte unit, byte function, byte exception)
        {
            if (_tcpSyncronousSocket == null && !SyncConnection) return;
            if (exception == excConnectionLost)
            {
                _tcpSyncronousSocket = null;
            }

            OnException?.Invoke(id, unit, function, exception);
        }

        internal static UInt16 SwapUInt16(UInt16 inValue)
        {
            return (UInt16)(((inValue & 0xff00) >> 8) |
                     ((inValue & 0x00ff) << 8));
        }

        /// <summary>
        /// Read Asyncronous coil values from slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="numInputs">Number of inputs to read</param>
        /// <param name="values">Result of the function</param>
        public void ReadCoilsAsync(ushort id, byte unit, ushort startAddress, ushort numInputs)
        {
            if (numInputs > 2000)
            {
                CallException(id, unit, _fctReadCoil, excIllegalDataValue);
                return;
            }
            WriteDataAsync(CreateReaderHeader(id, unit, startAddress, numInputs, _fctReadCoil), id);
        }

        private void WriteDataAsync(byte[] writeData, ushort id)
        {
            if ((_tcpASyncronousSocket != null) && (_tcpASyncronousSocket.Connected))
            {
                try
                {
                    _tcpASyncronousSocket.BeginSend(writeData, 0, writeData.Length, SocketFlags.None, new AsyncCallback(OnSendData), null);
                }
                catch (SystemException)
                {
                    CallException(id, writeData[6], writeData[7], excConnectionLost);
                }
            }
            else
            {
                CallException(id, writeData[6], writeData[7], excConnectionLost);
            }
        }

        // Write asyncronous data ack
        private void OnSendData(IAsyncResult result)
        {
            Int32 size = _tcpASyncronousSocket.EndSend(result);
            if (result.IsCompleted == false)
            {
                CallException(0xFFFF, 0xFF, 0xFF, excSendFailed);
            }
            else
            {
                _tcpASyncronousSocket.BeginReceive(_tcpASyncronousBufferArray, 0, _tcpASyncronousBufferArray.Length, SocketFlags.None, new AsyncCallback(OnDataReceive), _tcpASyncronousSocket);
            }
        }

        // Write asynchronous data response
        private void OnDataReceive(IAsyncResult result)
        {
            if (_tcpASyncronousSocket == null) return;

            try
            {
                _tcpASyncronousSocket.EndReceive(result);
                if (result.IsCompleted == false) CallException(0xFF, 0xFF, 0xFF, excConnectionLost);
            }
            catch (Exception) { }

            ushort id = SwapUInt16(BitConverter.ToUInt16(_tcpASyncronousBufferArray, 0));
            byte unit = _tcpASyncronousBufferArray[6];
            byte function = _tcpASyncronousBufferArray[7];
            byte[] data;

            // ------------------------------------------------------------
            // Write response data
            if ((function >= _fctWriteSingleCoil) && (function != _fctReadWriteMultipleRegister))
            {
                data = new byte[2];
                Array.Copy(_tcpASyncronousBufferArray, 10, data, 0, 2);
            }
            // ------------------------------------------------------------
            // Read response data
            else
            {
                data = new byte[_tcpASyncronousBufferArray[8]];
                Array.Copy(_tcpASyncronousBufferArray, 9, data, 0, _tcpASyncronousBufferArray[8]);
            }
            // ------------------------------------------------------------
            // Response data is slave exception
            if (function > excWrongOffset)
            {
                function -= excWrongOffset;
                CallException(id, unit, function, _tcpASyncronousBufferArray[8]);
            }
            // ------------------------------------------------------------
            // Response data is regular data
            else
            {
                OnResponseData?.Invoke(id, unit, function, data);
            }
        }



        /// <summary>
        /// Read syncronous coil values from slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="numInputs">Number of inputs to read</param>
        /// <param name="values">Result of the function</param>
        public void ReadCoils(ushort id, byte unit, ushort startAddress, ushort numInputs, ref byte[] values)
        {
            if (numInputs > 2000)
            {
                CallException(id, unit, _fctReadCoil, excIllegalDataValue);
                return;
            }
            values = WriteSyncData(CreateReaderHeader(id, unit, startAddress, numInputs, _fctReadCoil), id);
        }

        /// <summary>
        /// Read syncronous input values from slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="numInputs">Number of inputs to read</param>
        /// <param name="values">Result of the function</param>
        public void ReadDiscreteInputs(ushort id, byte unit, ushort startAddress, ushort numInputs, ref byte[] values)
        {
            if (numInputs > 2000)
            {
                CallException(id, unit, _fctReadDiscreteInputs, excIllegalDataValue);
                return;
            }
            values = WriteSyncData(CreateReaderHeader(id, unit, startAddress, numInputs, _fctReadDiscreteInputs), id);
        }

        /// <summary>
        /// Read syncronous holding register values from slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="numInputs">Number of registers to read</param>
        /// <param name="values">Result of the function</param>
        public void ReadHoldingRegisters(ushort id, byte unit, ushort startAddress, ushort numInputs, ref byte[] values)
        {
            if (numInputs > 125)
            {
                CallException(id, unit, _fctReadHoldingRegister, excIllegalDataValue);
                return;
            }
            values = WriteSyncData(CreateReaderHeader(id, unit, startAddress, numInputs, _fctReadHoldingRegister), id);
        }

        /// <summary>
        /// Read syncronous input register values from slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="numInputs">Number of registers to read</param>
        /// <param name="values">Result of the function</param>
        public void ReadInputRegisters(ushort id, byte unit, ushort startAddress, ushort numInputs, ref byte[] values)
        {
            if (numInputs > 125)
            {
                CallException(id, unit, _fctReadInputRegister, excIllegalDataValue);
                return;
            }
            values = WriteSyncData(CreateReaderHeader(id, unit, startAddress, numInputs, _fctReadInputRegister), id);
        }

        /// <summary>
        /// Write syncronous single coil values to slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="value">Specifies if the coil should be switched on or off. Accepts true or false values</param>
        /// <param name="result">Result of the function</param>
        public void WriteSingleCoil(ushort id, byte unit, ushort startAddress, bool value, ref byte[] result)
        {
            byte[] data;
            data = CreateWriteHeader(id, unit, startAddress, 1, 1, _fctWriteSingleCoil);
            if (value == true)
            {
                data[10] = 255;
            }
            else
            {
                data[10] = 0;
            }
            result = WriteSyncData(data, id);
        }

        /// <summary>
        /// Write syncronous multiple coil values to slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="numBits">Number of bits to write</param>
        /// <param name="values">Specifies the value of the register. Accepts 255 for true or 0 for false values</param>
        /// <param name="result">Result of the function</param>
        public void WriteMultipleCoils(ushort id, byte unit, ushort startAddress, ushort numBits, byte[] values, ref byte[] result)
        {
            ushort numBytes = Convert.ToUInt16(values.Length);
            if (numBytes > 250 || numBits > 2000)
            {
                CallException(id, unit, _fctWriteMultipleCoils, excIllegalDataValue);
                return;
            }

            byte[] data;
            data = CreateWriteHeader(id, unit, startAddress, numBits, (byte)(numBytes + 2), _fctWriteMultipleCoils);
            Array.Copy(values, 0, data, 13, numBytes);
            result = WriteSyncData(data, id);
        }

        /// <summary>
        /// Write syncronous single register values to slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="values">Specifies the value of the register</param>
        /// <param name="result">Result of the function</param>
        public void WriteSingleRegister(ushort id, byte unit, ushort startAddress, byte[] values, ref byte[] result)
        {
            if (values.GetUpperBound(0) != 1)
            {
                CallException(id, unit, _fctWriteSingleRegister, excIllegalDataValue);
                return;
            }
            byte[] data;
            data = CreateWriteHeader(id, unit, startAddress, 1, 1, _fctWriteSingleRegister);
            data[10] = values[0];
            data[11] = values[1];
            result = WriteSyncData(data, id);
        }

        /// <summary>
        /// Write syncronous multiple register values to slave
        /// </summary>
        /// <param name="id">Unique id that marks the transaction</param>
        /// <param name="unit">Slave address</param>
        /// <param name="startAddress">Addres from where the data read begin</param>
        /// <param name="values">Specifies the value of the register</param>
        /// <param name="result">Result of the function</param>
        public void WriteMultipleRegister(ushort id, byte unit, ushort startAddress, byte[] values, ref byte[] result)
        {
            ushort numBytes = Convert.ToUInt16(values.Length);
            if (numBytes > 250)
            {
                CallException(id, unit, _fctWriteMultipleRegistes, excIllegalDataValue);
                return;
            }

            if (numBytes % 2 > 0) numBytes++;
            byte[] data;

            data = CreateWriteHeader(id, unit, startAddress, Convert.ToUInt16(numBytes / 2), Convert.ToUInt16(numBytes + 2), _fctWriteMultipleRegistes);
            Array.Copy(values, 0, data, 13, values.Length);
            result = WriteSyncData(data, id);
        }

        private byte[] CreateReaderHeader(ushort id, byte unit, ushort startAddress, ushort lenght, byte function)
        {
            byte[] data = new byte[12];

            byte[] _id = BitConverter.GetBytes((short)id);
            data[0] = _id[1];
            data[1] = _id[0];

            data[2] = _id[0];
            data[3] = _id[0];
            data[4] = _id[0];

            data[5] = 6;
            data[6] = unit;
            data[7] = function;

            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startAddress));
            data[8] = _adr[0];
            data[9] = _adr[1];

            byte[] _lenght = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)lenght));
            data[10] = _lenght[0];
            data[11] = _lenght[1];

            return data;
        }

        private byte[] CreateWriteHeader(ushort id, byte unit, ushort startAddress, ushort numData, ushort numBytes, byte function)
        {
            byte[] data = new byte[numBytes + 11];

            byte[] _id = BitConverter.GetBytes((short)id);
            data[0] = _id[1];				// Slave id high byte
            data[1] = _id[0];				// Slave id low byte
            byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(5 + numBytes)));
            data[4] = _size[0];				// Complete message size in bytes
            data[5] = _size[1];				// Complete message size in bytes
            data[6] = unit;					// Slave address
            data[7] = function;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startAddress));
            data[8] = _adr[0];				// Start address
            data[9] = _adr[1];				// Start address
            if (function >= _fctWriteMultipleCoils)
            {
                byte[] _cnt = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)numData));
                data[10] = _cnt[0];			// Number of bytes
                data[11] = _cnt[1];			// Number of bytes
                data[12] = (byte)(numBytes - 2);
            }
            return data;
        }

        private byte[] WriteSyncData(byte[] data, ushort id)
        {
            if (_tcpSyncronousSocket.Connected)
            {
                try
                {
                    _tcpSyncronousSocket.Send(data, 0, data.Length, SocketFlags.None);
                    int result = _tcpSyncronousSocket.Receive(_tcpSyncronousBufferArray, 0, _tcpSyncronousBufferArray.Length, SocketFlags.None);

                    byte unit = _tcpSyncronousBufferArray[6];
                    byte function = _tcpSyncronousBufferArray[7];

                    byte[] reveivedData;

                    // check if connection lost
                    if (result == 0)
                    {
                        CallException(id, unit, data[7], excConnectionLost);
                    }

                    if (function > excWrongOffset)
                    {
                        function -= excWrongOffset;
                        CallException(id, unit, function, excWrongOffset);
                        return null;
                    }
                    else if ((function >= _fctWriteSingleCoil) && (function != _fctReadWriteMultipleRegister))
                    {
                        reveivedData = new byte[2];
                        Array.Copy(_tcpSyncronousBufferArray, 10, reveivedData, 0, 2);
                    }
                    // ------------------------------------------------------------
                    // Read response data
                    else
                    {
                        reveivedData = new byte[_tcpSyncronousBufferArray[8]];
                        Array.Copy(_tcpSyncronousBufferArray, 9, reveivedData, 0, _tcpSyncronousBufferArray[8]);
                    }
                    return reveivedData;
                }
                catch (SystemException)
                {
                    CallException(id, data[6], data[7], excConnectionLost);
                }
            }
            else
            {
                CallException(id, data[6], data[7], excConnectionLost);
            }
            return null;
        }
    }
}
