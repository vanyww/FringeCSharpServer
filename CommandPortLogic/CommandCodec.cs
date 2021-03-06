﻿using CommandPortLogic.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeCreator.Base;
using TypeCreator.Enums;

namespace CommandPortLogic
{
    public static class CommandCodec
    {
        public static OuterCommand DecodeCommandInit(Int32 identity, Byte[][] message)
        {
            Int32 index = 0;

            var command = new OuterCommand
            {
                CommandId = BitConverter.ToInt32(message[index++], 0),
                Name = Encoding.ASCII.GetString(message[index++]),
                Description = Encoding.ASCII.GetString(message[index++]),
                Usage = (CommandUsage)message[index++][0],
                Node = identity,
            };
            var msgDesc = GetPart();
            if (msgDesc is null)
                index++;
            var replDesc = GetPart();

            command.ReplyDescription = replDesc;
            command.RequestDescription = msgDesc;

            return command;

            List<MessagePartDescription> GetPart()
            {
                Byte[] name;
                MessagePartDescription msgPart;

                var part = new Lazy<List<MessagePartDescription>>();

                while (message.Length > index && (name = message[index++]).Length != 0)
                {
                    msgPart = new MessagePartDescription
                    {
                        Name = Encoding.ASCII.GetString(name),
                        Type = (MessageValueType)message[index++][0]
                    };

                    part.Value.Add(msgPart);
                }

                return part.IsValueCreated ? part.Value : null;
            }
        }

        public static CommandReply DecodeReplyIds(Int32 identity, Byte[][] msg) =>
            new CommandReply
            {
                CallId = BitConverter.ToInt32(msg[2], 0),
                CommandId = BitConverter.ToInt32(msg[0], 0)
            };

        public static CommandReply DecodeReplyFully(CommandReply partialDecodedReply, OuterCommandCall commandCall, Byte[][] msg)
        {
            partialDecodedReply.Result = commandCall.Command.MessageWrapper.CreateReply(msg.Skip(3).ToArray());
            return partialDecodedReply;
        }

        public static (Int32, Byte[][]) EncodeCommandCall(OuterCommandCall commandCall)
        {
            var requestMessageBytes = commandCall.CallMessage?.GetPropertiesBytes();
            var requestSize = requestMessageBytes?.Length ?? 0;
            var encodeTemp = new Byte[3 + requestSize][];

            encodeTemp[0] = new[] { (Byte)CommandMessageSubtype.Request };
            encodeTemp[1] = BitConverter.GetBytes(commandCall.Command.CommandId);
            encodeTemp[2] = BitConverter.GetBytes(commandCall.CallId);

            for (Int32 i = 3, k = 0; k < requestSize; i++, k++)
                encodeTemp[i] = requestMessageBytes[k];

            return (commandCall.Command.Node, encodeTemp.ToArray());
        }
    }
}
