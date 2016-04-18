﻿using System;
using System.Collections.Generic;
using MatrixSDK.Structures;
namespace MatrixSDK.Client
{
	
	public delegate void MatrixRoomEventDelegate(MatrixRoom room,MatrixEvent evt);

	/// <summary>
	/// A room that the user has joined on Matrix.
	/// </summary>
	public class MatrixRoom
	{

		const int MESSAGE_CAPACITY = 255;

		/// <summary>
		/// The server assigned ID for the room. This can never change.
		/// </summary>
		public readonly string ID;
		public string Name { get; private set; }
		public string Topic { get; private set; }
		public string Creator { get; private set; }

		/// <summary>
		/// Should this Matrix Room federate with other home servers?
		/// </summary>
		/// <value><c>true</c> if should federate; otherwise, <c>false</c>.</value>
		public bool ShouldFederate { get; private set; }
		public string CanonicalAlias { get; private set; }
		public string[] Aliases { get; private set; }

		public EMatrixRoomJoinRules JoinRule { get; private set; }
		public MatrixMRoomPowerLevels PowerLevels { get; private set; }

		/// <summary>
		/// Occurs when a m.room.message is recieved. 
		/// <remarks>This will include your own messages</remarks>
		/// </summary>
		public event MatrixRoomEventDelegate OnMessage;

		/// <summary>
		/// Fires when any room message is recieved.
		/// </summary>
		public event MatrixRoomEventDelegate OnEvent;

		/// <summary>
		/// Don't fire OnMessage if the message exceeds this age limit (in milliseconds).
		/// </summary>
		public int OnMessageMaximumAge = 5000;

		private List<MatrixMRoomMessage> messages = new List<MatrixMRoomMessage>(MESSAGE_CAPACITY);

		/// <summary>
		/// Get a list of all the messages recieved so far.
		/// <remarks>This is not a complete list for the rooms entire history</remarks>
		/// </summary>
		public MatrixMRoomMessage[] Messages { get { return messages.ToArray (); } }

		private MatrixAPI api;

		/// <summary>
		/// This constructor is intended for the API only.
		/// Initializes a new instance of the <see cref="MatrixSDK.Client.MatrixRoom"/> class.
		/// </summary>
		/// <param name="API">The API to send/recieve requests from</param>
		/// <param name="roomid">Roomid</param>
		public MatrixRoom (MatrixAPI API,string roomid)
		{
			ID = roomid;
			api = API;
		}

		/// <summary>
		/// This method is intended for the API only.
		/// If a Room recieves a new event, process it in here.
		/// </summary>
		/// <param name="evt">New event</param>
		public void FeedEvent(MatrixEvent evt){
			Type t = evt.content.GetType();
			if (t == typeof(MatrixMRoomCreate)) {
				Creator = ((MatrixMRoomCreate)evt.content).creator;
			} else if (t == typeof(MatrixMRoomName)) {
				Name = ((MatrixMRoomName)evt.content).name;
			} else if (t == typeof(MatrixMRoomTopic)) {
				Topic = ((MatrixMRoomTopic)evt.content).topic;
			} else if (t == typeof(MatrixMRoomAliases)) {
				Aliases = ((MatrixMRoomAliases)evt.content).aliases;
			} else if (t == typeof(MatrixMRoomCanonicalAlias)) {
				CanonicalAlias = ((MatrixMRoomCanonicalAlias)evt.content).alias;
			} else if (t == typeof(MatrixMRoomJoinRules)) {
				JoinRule = ((MatrixMRoomJoinRules)evt.content).join_rule;
			} else if (t == typeof(MatrixMRoomJoinRules)) {
				PowerLevels = ((MatrixMRoomPowerLevels)evt.content);
			} else if (t.IsSubclassOf(typeof(MatrixMRoomMessage))) {
				messages.Add ((MatrixMRoomMessage)evt.content);
				if (OnMessage != null ) {
					if(OnMessageMaximumAge == 0 || evt.age < OnMessageMaximumAge )
					try
					{
						OnMessage.Invoke (this, evt);
					}
					catch(Exception e){
						Console.WriteLine ("A OnMessage handler failed");
						Console.WriteLine (e);
					}
				}
			}

			if (OnEvent != null) {
				OnEvent.Invoke (this, evt);
			}
		}

		/// <summary>
		/// Attempt to set the name of the room.
		/// This may fail if you do not have the required permissions.
		/// </summary>
		/// <param name="newName">New name.</param>
		public void SetName(string newName){
			MatrixMRoomName nameEvent = new MatrixMRoomName ();
			nameEvent.name = newName;
			api.RoomStateSend (ID, "m.room.name", nameEvent); 
		}

		/// <summary>
		/// Attempt to set the topic of the room.
		/// This may fail if you do not have the required permissions.
		/// </summary>
		/// <param name="newTopic">New topic.</param>
		public void SetTopic(string newTopic){
			MatrixMRoomTopic topicEvent = new MatrixMRoomTopic ();
			topicEvent.topic = newTopic;
			api.RoomStateSend (ID, "m.room.topic", topicEvent);
		}

		/// <summary>
		/// Send a new message to the room.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendMessage(MatrixMRoomMessage message){
			api.RoomMessageSend (ID, "m.room.message", message);
		}

		/// <summary>
		/// Send a MMessageText message to the room.
		/// </summary>
		/// <param name="body">The string body of the message</param>
		public void SendMessage(string body){
			MMessageText message = new MMessageText ();
			message.body = body;
			SendMessage (message);
		}

		/// <summary>
		/// Applies the new power levels.
		/// <remarks> You must set all the values in powerlevels.</remarks>
		/// </summary>
		/// <param name="powerlevels">Powerlevels.</param>
		public void ApplyNewPowerLevels(MatrixMRoomPowerLevels powerlevels){
			api.RoomStateSend (ID,"m.room.power_levels",powerlevels);
		}

		/// <summary>
		/// Invite a user to the room by userid.
		/// </summary>
		/// <param name="userid">Userid.</param>
		public void InviteToRoom(string userid){
			api.InviteToRoom (ID, userid);
		}

		/// <summary>
		/// Invite a user to the room by their object.
		/// </summary>
		/// <param name="user">User.</param>
		public void InviteToRoom(MatrixUser user){
			InviteToRoom (user.UserID);
		}

		/// <summary>
		/// Leave the room on the server.
		/// </summary>
		public void LeaveRoom(){
			api.RoomLeave (ID);
		}

	}
}

