﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECore.DeviceMemories
{
    //this class defines which type of registers it contain, how much of them, and how to access them
    //actual filling of these registers must be defined by the specific HWImplementation, through the constructor of this class
    public class Scop3PICRegisterMemory: EDeviceMemory
    {       
        //this method defines which type of registers are stored in the memory
        public Scop3PICRegisterMemory(EDevice eDevice, Dictionary<string, int> registerNames)
        {
            this.eDevice = eDevice;
            this.registerIndices = registerNames;
                        
            //look up how many registers are required
            int largestIndex = 0;
            foreach (KeyValuePair<string, int> kvp in registerNames)
                if (kvp.Value > largestIndex) 
                    largestIndex = kvp.Value;

            //instantiate registerList
            registers = new List<EDeviceMemoryRegister>();
            for (int i = 0; i < largestIndex+1; i++)
            {
                //find name of this register
                string regName = "<none>";
                foreach (KeyValuePair<string, int> kvp in registerNames)
                    if (kvp.Value == i)
                        regName = kvp.Key;

                registers.Add(new MemoryRegisters.ByteRegister(regName, this));
            }

        }

        public override void ReadRange(int startAddress, int burstSize)
        {            
            ////////////////////////////////////////////////////////
            //first initiate i2c write to send FPGA I2C address and register to read from
            byte[] toSend1 = new byte[4];
            //prep header
            int i = 0;
            toSend1[i++] = 123; //message for PIC
            toSend1[i++] = 3; //HOST_COMMAND_GET_PIC_REGISTER
            toSend1[i++] = (byte)(startAddress); 
            toSend1[i++] = (byte)(burstSize); 

            //send this over, so FPGA register pointer is set to correct register
            eDevice.HWInterface.WriteControlBytes(toSend1);

            //now data is stored in EP3 of PIC, so read it
            byte[] readBuffer = eDevice.HWInterface.ReadControlBytes(16); //EP3 always contains 16 bytes xxx should be linked to constant

            //strip away first 4 bytes (as these are not data) and store inside registers
            byte[] returnBuffer = new byte[burstSize];
            for (int j = 0; j < burstSize; j++)
                registers[startAddress + j].InternalValue = readBuffer[4 + j];
        }

        public override void WriteRange(int startAddress, int burstSize)
        {
            byte[] toSend = new byte[burstSize + 4];

            //prep header
            int i = 0;
            toSend[i++] = 123; //message for FPGA
            toSend[i++] = 2; //HOST_COMMAND_SET_PIC_REGISTER
            toSend[i++] = (byte)(startAddress); 
            toSend[i++] = (byte)(burstSize); //first I2C byte: FPGA i2c address (5) + '0' as LSB, indicating write operation

            //append the actual data
            for (int j = 0; j < burstSize; j++)
                toSend[i++] = this.registers[startAddress + j].InternalValue;

            eDevice.HWInterface.WriteControlBytes(toSend);
        }

    }
}
