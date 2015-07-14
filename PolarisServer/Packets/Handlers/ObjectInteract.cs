﻿using PolarisServer.Database;
using PolarisServer.Models;
using PolarisServer.Object;
using PolarisServer.Packets.PSOPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PolarisServer.Packets.Handlers
{
    [PacketHandlerAttr(0x4, 0x14)]
    class ObjectInteract : PacketHandler
    {
        public override void HandlePacket(Client context, byte[] data, uint position, uint size)
        {
            PacketReader reader = new PacketReader(data);
            reader.ReadBytes(12); // Padding MAYBE???????????
            EntityHeader srcObject = reader.ReadStruct<EntityHeader>();
            byte[] someBytes = reader.ReadBytes(4); // Dunno what this is yet.
            EntityHeader dstObject = reader.ReadStruct<EntityHeader>(); // Could be wrong
            reader.ReadBytes(16); // Not sure what this is yet
            string command = reader.ReadAscii(0xD711, 0xCA);
            PSOObject srcObj;
            if(srcObject.EntityType == EntityType.Object)
            {
                srcObj = ObjectManager.Instance.getObjectByID(context.CurrentZone, srcObject.ID);
            }
            else if(srcObject.EntityType == EntityType.Player)
            {
                srcObj = new PSOObject();
                srcObj.Header = srcObject;
                srcObj.Name = "Player";
            }
            else
            {
                srcObj = null;
            }

            Logger.WriteInternal("[OBJ] {0} (ID {1}) <{2}> --> Ent {3} (ID {4})", srcObj.Name, srcObj.Header.ID, command, (EntityType)dstObject.EntityType, dstObject.ID);

            // TODO: Delete this code and do this COMPLETELY correctly!!!
            if (command == "Transfer" && context.CurrentZone == "lobby")
            {
                // Try and get the teleport definition for the object...
                using (var db = new PolarisEf())
                {
                    db.Configuration.AutoDetectChangesEnabled = true;
                    var teleporterEndpoint = db.Teleports.Find("lobby", (int)srcObject.ID);

                    if (teleporterEndpoint == null)
                    {
                        Logger.WriteError("[OBJ] Teleporter for {0} in {1} does not contain a destination!", srcObj.Header.ID, "lobby");
                        // Teleport Player to default point
                        context.SendPacket(new TeleportTransferPacket(srcObj, new PSOLocation(0f, 1f, 0f, -0.000031f, -0.417969f, 0.000031f, 134.375f)));
                        // Unhide player
                        context.SendPacket(new ObjectActionPacket(dstObject, srcObject, new EntityHeader(), new EntityHeader(), "Forwarded"));
                    }
                    else
                    {
                        PSOLocation endpointLocation = new PSOLocation()
                        {
                            RotX = teleporterEndpoint.RotX,
                            RotY = teleporterEndpoint.RotY,
                            RotZ = teleporterEndpoint.RotZ,
                            RotW = teleporterEndpoint.RotW,
                            PosX = teleporterEndpoint.PosX,
                            PosY = teleporterEndpoint.PosY,
                            PosZ = teleporterEndpoint.PosZ,
                        };
                        // Teleport Player
                        context.SendPacket(new TeleportTransferPacket(srcObj, endpointLocation));
                        // Unhide player
                        context.SendPacket(new ObjectActionPacket(dstObject, srcObject, new EntityHeader(), new EntityHeader(), "Forwarded")); 
                    }
                }
            }
        }
    }

}
