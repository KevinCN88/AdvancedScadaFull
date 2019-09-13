﻿using AdvancedScada.DriverBase.DataTypes;
using AdvancedScada.DriverBase.Devices;
using AdvancedScada.IODriverV2.Comm;
using System;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using static AdvancedScada.IBaseService.Common.XCollection;
namespace AdvancedScada.IODriverV2.XDelta.TCP
{
    public class DVPTCPMaster : DVPTCPMessage, IDriverAdapterV2
    {
        private const int DELAY = 10;


        private EthernetAdapter EthernetAdaper;
        private SerialPortAdapter SerialAdaper;
        public bool _IsConnected = false;
        public bool IsConnected
        {
            get
            {
                return _IsConnected;
            }

            set
            {
                _IsConnected = value;
            }
        }
        private short slaveId;
        public bool IsAvailable
        {
            get
            {
                try
                {
                    Connection();

                    return IsConnected;


                }
                catch
                {
                    return false;
                }
            }
        }

        public DVPTCPMaster()
        {
        }

        public DVPTCPMaster(short slaveId, string ip, int port)
            : this()
        {
            this.slaveId = slaveId;
            EthernetAdaper = new EthernetAdapter(ip, port);
        }

        public DVPTCPMaster(string ip, short port, int connectTimeout)
            : this()
        {
            EthernetAdaper = new EthernetAdapter(ip, port, connectTimeout);
        }

        public void Connection()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                IsConnected = EthernetAdaper.Connect();

                stopwatch.Stop();
            }
            catch (SocketException ex)
            {
                stopwatch.Stop();
                EventscadaException?.Invoke(this.GetType().Name,
                     $"Could Not Connect to Server : {ex.SocketErrorCode} Time: {stopwatch.ElapsedTicks}");
                IsConnected = false;


            }
        }

        public void Disconnection()
        {
            try
            {
                EthernetAdaper.Close();

                IsConnected = false;
            }
            catch (SocketException)
            {
            }
            finally
            {

            }
        }

        public byte[] ReadCoilStatus(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            var stopwatch = Stopwatch.StartNew();

            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = ReadCoilStatusMessage(slaveAddress, $"{Address}", nuMBErOfPoints);
            stopwatch.Stop();

            EthernetAdaper.Write(frame);
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_01 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            int SizeByte = buffReceiver[8]; // Số lượng byte dữ liệu thu được.
            var data = new byte[SizeByte];
            Array.Copy(buffReceiver, 9, data, 0,
                data.Length); // Dữ liệu cần lấy bắt đầu từ byte có chỉ số 9 trong buffReceive.            
            return Bit.ToByteArray(Bit.ToArray(data));
        }

        public byte[] ReadInputStatus(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = ReadInputStatusMessage(slaveAddress, $"{Address}", nuMBErOfPoints);
            EthernetAdaper.Write(frame);
            // RequestAndResponseMessage _RequestAndResponseMessage = new RequestAndResponseMessage("RequestRead", frame);

            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_02 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            int SizeByte = buffReceiver[8]; // Số lượng byte dữ liệu thu được.
            var data = new byte[SizeByte];
            Array.Copy(buffReceiver, 9, data, 0,
                data.Length); // Dữ liệu cần lấy bắt đầu từ byte có chỉ số 9 trong buffReceive.            
            return Bit.ToByteArray(Bit.ToArray(data));
        }

        public byte[] ReadHoldingRegisters(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = ReadHoldingRegistersMessage(slaveAddress, $"{Address}", nuMBErOfPoints);
            EthernetAdaper.Write(frame);
 
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_03 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            int SizeByte = buffReceiver[8]; // Số lượng byte dữ liệu thu được.
            var data = new byte[SizeByte];
            Array.Copy(buffReceiver, 9, data, 0,
                data.Length); // Dữ liệu cần lấy bắt đầu từ byte có chỉ số 9 trong buffReceive.            
            return data;
        }

        public byte[] ReadInputRegisters(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = ReadInputRegistersMessage(slaveAddress, $"{Address}", nuMBErOfPoints);
            EthernetAdaper.Write(frame);
 
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_04 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            int SizeByte = buffReceiver[8]; // Số lượng byte dữ liệu thu được.
            var data = new byte[SizeByte];
            Array.Copy(buffReceiver, 9, data, 0,
                data.Length); // Dữ liệu cần lấy bắt đầu từ byte có chỉ số 9 trong buffReceive.            
            return data;
        }

        public byte[] WriteSingleCoil(byte slaveAddress, string startAddress, bool value)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = WriteSingleCoilMessage(slaveAddress, $"{Address}", value);
            EthernetAdaper.Write(frame);
 
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_05 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            return buffReceiver;
        }

        public byte[] WriteMultipleCoils(byte slaveAddress, string startAddress, bool[] values)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = WriteMultipleCoilsMessage(slaveAddress, $"{Address}", values);
            EthernetAdaper.Write(frame);
 
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_15 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            return buffReceiver;
        }

        public byte[] WriteSingleRegister(byte slaveAddress, string startAddress, byte[] values)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = WriteSingleRegisterMessage(slaveAddress, $"{Address}", values);
            EthernetAdaper.Write(frame);
 
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_06 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            return buffReceiver;
        }

        public byte[] WriteMultipleRegisters(byte slaveAddress, string startAddress, byte[] values)
        {
            var Address = DMT.DevToAddrW("DVP", startAddress, slaveAddress);
            var frame = WriteMultipleRegistersMessage(slaveAddress, $"{Address}", values);
            EthernetAdaper.Write(frame);
 
            Thread.Sleep(DELAY);
            var buffReceiver = EthernetAdaper.Read();
            if (FUNCTION_16 != buffReceiver[7])
            {
                var errorbytes = new byte[3];
                Array.Copy(buffReceiver, 6, errorbytes, 0, errorbytes.Length);
                ModbusExcetion(errorbytes);
            }

            return buffReceiver;
        }

        public void AllSerialPortAdapter(SerialPortAdapter iModbusSerialPortAdapter)
        {
            SerialAdaper = iModbusSerialPortAdapter;
        }

        public void AllEthernetAdapter(EthernetAdapter iModbusEthernetAdapter)
        {
            EthernetAdaper = iModbusEthernetAdapter;
        }

        public ConnectionState GetConnectionState()
        {
            throw new NotImplementedException();
        }

        public byte[] BuildReadByte(byte station, string address, ushort length)
        {
            throw new NotImplementedException();
        }

        public byte[] BuildWriteByte(byte station, string address, byte[] value)
        {
            throw new NotImplementedException();
        }

        public TValue[] Read<TValue>(string address, ushort length)
        {
            if (typeof(TValue) == typeof(bool))
            {
                var b = Bit.ToArray(ReadCoilStatus((byte)slaveId, address, length));
                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(ushort))
            {
                var b = Word.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));

                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(int))
            {
                var b = Int.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));

                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(uint))
            {
                var b = DInt.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));
                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(long))
            {
                var b = DWord.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));
                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(ulong))
            {
                var b = DInt.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));
                return (TValue[])(object)b;
            }

            if (typeof(TValue) == typeof(short))
            {
                var b = Word.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));
                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(double))
            {
                var b = Real.ToArrayInverse(ReadHoldingRegisters((byte)slaveId, address, length));
                return (TValue[])(object)b;
            }
            if (typeof(TValue) == typeof(float))
            {
                var b = Real.ToArray(ReadHoldingRegisters((byte)slaveId, address, length));
                return (TValue[])(object)b;

            }
            if (typeof(TValue) == typeof(string))
            {
                var b = string.Empty;
                return (TValue[])(object)b;
            }

            throw new InvalidOperationException(string.Format("type '{0}' not supported.", typeof(TValue)));
        }

        public TValue[] Read<TValue>(DataBlock db)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadDiscrete(string address, ushort length)
        {
            var b = Bit.ToArray(ReadInputStatus((byte)slaveId, address, length));
            return b;
        }

        public bool Write(string address, dynamic value)
        {
            if (value is bool)
            {
                WriteSingleCoil((byte)slaveId, address, value);
            }
            else
            {
                WriteSingleRegister((byte)slaveId, address, value);
            }

            return true;
        }
    }
}