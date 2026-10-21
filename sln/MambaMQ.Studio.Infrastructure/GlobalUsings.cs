global using Microsoft.Extensions.DependencyInjection;

global using System.Net.Sockets;
global using System.Buffers.Binary;
global using System.Security.Authentication;

global using MambaMQ.Studio.Infrastructure.Client;
global using MambaMQ.Studio.Infrastructure.Options;
global using MambaMQ.Studio.Infrastructure.Readers;
global using MambaMQ.Studio.Infrastructure.Connection;
global using MambaMQ.Studio.Infrastructure.Services.Abstractions;
global using MambaMQ.Studio.Infrastructure.Services.Implementations;

global using MambaMQ.Studio.Persistence.Models;
global using MambaMQ.Studio.Persistence.Services;
global using MambaMQ.Studio.Persistence.Extensions;

global using MambaMQ.Protocol.Enums;
global using MambaMQ.Protocol.Frames;
global using MambaMQ.Protocol.Commands;
global using MambaMQ.Protocol.Messages;
global using MambaMQ.Protocol.Constants;
global using MambaMQ.Protocol.Responses;
global using MambaMQ.Protocol.Serialization.Frames;
global using MambaMQ.Protocol.Serialization.Commands;
global using MambaMQ.Protocol.Responses.Models.Queues;
global using MambaMQ.Protocol.Serialization.Responses;
global using MambaMQ.Protocol.Responses.Models.Messages;
global using MambaMQ.Protocol.Serialization.Responses.Queues;
global using MambaMQ.Protocol.Serialization.Responses.Messages;
global using MambaMQ.Protocol.Responses.Models.Exchanges;
global using MambaMQ.Protocol.Serialization.Responses.Exchanges;