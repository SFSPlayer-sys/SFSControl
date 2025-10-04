using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SFS.World;

namespace SFSControl
{
	public enum DrawCommandType
	{
		Line,
		Circle,
		Text,
		Clear
	}

	public class DrawCommand
	{
		public DrawCommandType type;
		public Vector3 start;
		public Vector3 end;
		public Vector2 center;
		public float radius;
		public int resolution = 64;
		public Color color = Color.white;
		public float width = 1f;
		public float sortingOrder = 1f;

		// text
		public string text;
		public int fontSize = 18;
        public float rotationDeg = 0f; 
	}

	public class DrawManager : MonoBehaviour, I_GLDrawer
	{
		private readonly object locker = new object();
		private readonly List<DrawCommand> persistent = new List<DrawCommand>();
		private readonly Queue<DrawCommand> incoming = new Queue<DrawCommand>();

		public static DrawManager main;

		private void Awake()
		{
			main = this;
		}

		public static void Ensure()
		{
			if (main == null)
			{
				var go = new GameObject("SFSControl DrawManager");
				DontDestroyOnLoad(go);
				main = go.AddComponent<DrawManager>();
			}
		}

		public void Enqueue(DrawCommand cmd)
		{
			lock (locker)
			{
				incoming.Enqueue(cmd);
			}
		}

		void Update()
		{
			// Register with global GL drawer when available
			if (!(GLDrawer.main is null))
			{
				if (!GLDrawer.main.drawers.Contains(this))
					GLDrawer.Register(this);
			}

			// Move incoming to persistent on main thread
            lock (locker)
			{
				while (incoming.Count > 0)
				{
					var cmd = incoming.Dequeue();
					if (cmd.type == DrawCommandType.Clear)
					{
						persistent.Clear();
						continue;
					}
					persistent.Add(cmd);
				}
			}
		}

		void I_GLDrawer.Draw()
		{
			// Draw all persistent commands
			for (int i = 0; i < persistent.Count; i++)
			{
				var c = persistent[i];
				switch (c.type)
				{
					case DrawCommandType.Line:
					{
						Vector3 a = ToLocal(c.start);
						Vector3 b = ToLocal(c.end);
						GLDrawer.DrawLine(a, b, c.color, c.width, c.sortingOrder);
					}
						break;
					case DrawCommandType.Circle:
					{
						Vector2 p = ToLocal(c.center);
						GLDrawer.DrawCircle(p, c.radius, Mathf.Max(8, c.resolution), c.color, c.sortingOrder);
					}
						break;
					case DrawCommandType.Text:
                        // Not supported in GL-only mode
						break;
				}
			}
		}

		private static Vector3 ToLocal(Vector3 world)
		{
			var mv = WorldView.main;
			if (mv == null) return world;
			var off = mv.positionOffset.Value;
			return new Vector3((float)(world.x - off.x), (float)(world.y - off.y), 0f);
		}

		private static Vector2 ToLocal(Vector2 world)
		{
			var mv = WorldView.main;
			if (mv == null) return world;
			var off = mv.positionOffset.Value;
			return new Vector2((float)(world.x - off.x), (float)(world.y - off.y));
		}
        // No screen-space conversion; inputs are in world coordinates
	}
}