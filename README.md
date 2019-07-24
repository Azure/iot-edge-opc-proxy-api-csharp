This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[![Build Status](https://travis-ci.org/Azure/iot-edge-opc-proxy-api-csharp.svg?branch=master)](https://travis-ci.org/Azure/iot-edge-opc-proxy-api-csharp)

# OPC Proxy API

Using the OPC Proxy API, client applications can connect to devices in a local gateway network and exchange transparent payloads, allowing developers to implement applications in Azure where the command and control protocol layer resides in the cloud. 

The OPC proxy edge module itself can be found at https://github.com/Azure/iot-edge-opc-proxy/tree/master.

# Getting started

> Before you run any of the included samples, you must obtain a service manage *connection string* for your IoT Hub. You can use the *iothubowner* connection string (going forward referred to as **<*iothubownerconnectionstring>**) which can easily be found on the [Azure portal](https://portal.azure.com) in the "Shared Access Policies" section of the [IoT hub settings blade](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#change-the-settings-of-the-iot-hub). For more information checkout out the documentation [here](https://github.com/Azure/azure-iot-device-ecosystem/blob/master/setup_iothub.md).

You also need to start the proxy gateway itself.  You can run it using [docker](https://www.docker.com/get-docker) (```docker run -h myproxy -it microsoft/iot-edge-opc-proxy -c "<*iothubownerconnectionstring>"```) or by following the instructions at https://github.com/Azure/iot-edge-opc-proxy.

# Samples

> For simplicity, all samples read the <*iothubownerconnectionstring> from the  ```_HUB_CS ``` environment variable.  If you do not set this variable on your machine the included samples will not work!  However, make sure you safeguard the connection string properly on your development machine and do not develop against a production IoT Hub.    

The following samples are included to demonstrate the use of the .net API:

- An [OPC UA client](/samples/opc-ua/readme.md) that shows how the [OPC-Foundation reference stack](https://github.com/OPCFoundation/UA-.NETStandardLibrary) can be used to relay OPC UA from the cloud to machines in a local gateway network. 

# Support and Contributions

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

If you are having issues compiling or using the code in this project please feel free to log an issue in the [issues section](https://github.com/Azure/iot-edge-opc-proxy-api-csharp/issues) of this project.

For other issues, such as Connectivity issues or problems with the portal, or issues using the Azure IoT Hub service the Microsoft Customer Support team will try and help out on a best effort basis.
To engage Microsoft support, you can create a support ticket directly from the [Azure portal](https://ms.portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade).

# License

The Azure IoT OPC Proxy module is licensed under the [MIT License](https://github.com/Azure/iot-edge-opc-proxy-api-csharp/blob/master/LICENSE). 

Visit http://azure.com/iotdev to learn more about developing applications for Azure IoT.
