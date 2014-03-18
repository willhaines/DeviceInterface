﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace ECore
{
    //ideally would be to constrain interfaces to be applied only to AuxFunctionalities class
    //how to use generics for memory? after propagation, EDevice needs to have a List of memories, so it needs to know all the specific memory types. or just use an arraylist?
    //strobes could be a parameter, as it is built on other registers. or an aux functionality!
    //parameters should define affected memories, and their registers as strings

    //main class, from which all specific cameras inherit
    public class EDevice
    {
        //properties regarding camera
        private EDeviceHWInterface hardwareInterface;
        private EDeviceImplementation deviceImplementation;
        private DeviceImplementations.Scop3v2.Scop3v2RomManager romManager;
        
        //properties regarding thread management
        private Thread dataFetchThread;
        private bool isRunning;
        private EDataNode dataGeneratorNode;

		//events
		public event NewDataAvailableHandler OnNewDataAvailable;

#if false
		#if ANDROID
		public static Android.Content.Context ApplicationContext;
		public EDevice(Type deviceImplementationType, Android.Content.Context appContext): this(deviceImplementationType)
		{
			ApplicationContext = appContext;
		}
		#endif        
#endif

        public EDevice(Type deviceImplementationType)
        {
            //creates an instance of the selected cameraImplementation
            //object[] parameters = {this};
            //this.deviceImplementation = (EDeviceImplementation)Activator.CreateInstance(deviceImplementationType, parameters); ;
            this.deviceImplementation = new DeviceImplementations.Scop3v2(this);
            this.hardwareInterface = deviceImplementation.CreateHWInterface();
            this.romManager = deviceImplementation.CreateRomManager();

            this.isRunning = false;

            Logger.AddEntry(this, LogMessageType.ECoreInfo, "EDevice initialized");
        }

        //start new thread, which will only fetch new data
        public void Start()
        {
            isRunning = true;            

            //check whether physical HW device is connected. if not, load data from a stream
			if (HWInterface.Connected)
                //load data from a device
                dataGeneratorNode = new EDataNodes.EDataNodeFromDevice(this);
            else
                //load data from a stream                
                dataGeneratorNode = new EDataNodes.EDataNodeFromFile();

            //create and start thread, operating on dataGeneratorNode
            dataFetchThread = new Thread(RunThreadDataGenerator);
            dataFetchThread.Name = "DataFetchFromDeviceThread";
            dataFetchThread.Priority = ThreadPriority.AboveNormal;
            dataFetchThread.Start();
        }

        public void StartFromEmbedded()
        {
            isRunning = true;

            //check whether physical HW device is connected. if not, load data from a stream
            
            dataGeneratorNode = new EDataNodes.EDataNodeFromEmbeddedResource();

            //create and start thread, operating on dataGeneratorNode
            dataFetchThread = new Thread(RunThreadDataGenerator);
            dataFetchThread.Name = "DataFetchFromDeviceThread";
            dataFetchThread.Priority = ThreadPriority.AboveNormal;
            dataFetchThread.Start();
        }

        public void RunThreadDataGenerator()
        {           
            //main starting point for the thread which fetches the data from file
            Logger.AddEntry(this, LogMessageType.ECoreInfo, "DataFetchThread spawn");

            //start HW
            hardwareInterface.StartInterface();
            deviceImplementation.StartDevice();

            //looping until device is stopped
            while (isRunning)
            {
                //update data
                dataGeneratorNode.Update(null, null);

                //flag that new data has arrived
                if (OnNewDataAvailable != null)
                    OnNewDataAvailable(dataGeneratorNode,  new EventArgs());

                //Stop();
            }
        }

        public void Stop()
        {
            //stops acquisition thread
            isRunning = false;

            //stop HW
            //dataFetchThread.Join(); --> We should do this here but it causes deadlock cos of logging not being asynchronous!!!
            deviceImplementation.StopDevice();
            hardwareInterface.StopInterface();

            //add entry to log
            Logger.AddEntry(this, LogMessageType.ECoreInfo, "DataFetchThread stopped now");
        }

        public EDeviceHWInterface HWInterface { get { return this.hardwareInterface; } }
        public EDeviceImplementation DeviceImplementation { get { return this.deviceImplementation; } }
        public DeviceImplementations.Scop3v2.Scop3v2RomManager RomManager { get { return this.romManager; } }
        public bool IsRunning { get { return isRunning; } }

        /* Settings handlers */
        static public String SettingSetterMethodName(Setting s)
        {
            String methodName = "Set" + Utils.SnakeToCamel(Enum.GetName(s.GetType(), s));
            return methodName;
        }

        public bool HasSetting(Setting s)
        {
            return this.deviceImplementation.HasSetting(s);
        }

        public void Set(Setting s, Object[] parameters) {
            if (!this.deviceImplementation.HasSetting(s))
                throw new MissingSettingException("The setting " + Enum.GetName(s.GetType(), s) + " is not implemetend by this device");
            MethodInfo m = this.deviceImplementation.GetType().GetMethod(SettingSetterMethodName(s));
            m.Invoke(this.deviceImplementation, parameters);
        }
    }
}

