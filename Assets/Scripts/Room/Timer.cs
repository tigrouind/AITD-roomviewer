﻿using System;
using UnityEngine;

public class Timer
{
	private float started;
	private bool running;
	private float elapsed;

	public void Start()
	{
		if (!running)
		{
			started = Time.time;
			running = true;
		}
	}

	public void Stop()
	{
		if (running)
		{
			elapsed += Time.time - started;
			running = false;
		}
	}

	public void Restart()
	{
		elapsed = 0.0f;
		started = Time.time;
		running = true;
	}

	public void Reset() {
		elapsed = 0.0f;
		running = false;
	}

	public float Elapsed
	{
		get
		{
			float time = elapsed;
			if (running)
			{
				time += Time.time - started;
			}

			return time;
		}

		set
		{
			if (running)
			{
				started = Time.time;
			}

			elapsed = value;
		}
	}
}