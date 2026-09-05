global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.DependencyInjection;

global using System.Net;
global using System.Net.Sockets;
global using System.Buffers.Binary;
global using System.Reflection;
global using System.Runtime.CompilerServices;

global using MambaMQ.Server.Server;
global using MambaMQ.Server.Queues;
global using MambaMQ.Server.Options;
global using MambaMQ.Server.Helpers;
global using MambaMQ.Server.Readers;
global using MambaMQ.Server.Handlers;
global using MambaMQ.Server.Dispatchers;
global using MambaMQ.Server.Connections;
global using MambaMQ.Server.QueueManagers;
global using MambaMQ.Server.Handlers.Abstractions;

global using MambaMQ.Protocol.Frames;
global using MambaMQ.Protocol.Messages;
global using MambaMQ.Protocol.Commands;
global using MambaMQ.Protocol.Constants;
global using MambaMQ.Protocol.Commands.Abstractions;
global using MambaMQ.Protocol.Serialization.Commands;
global using MambaMQ.Protocol.Serialization.Frames;
global using MambaMQ.Protocol.Serialization.Messages;