using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	[RequireComponent(typeof(AudioSource))]
	public class AudioEmitter : MonoBehaviour
	{
		enum State
		{
			ON_ENABLE,
			ON_DISABLE,
			NONE
		}

		[SerializeField] private State startAction = State.NONE;
		[SerializeField] private State stopAction = State.NONE;

		[SerializeField] private AudioClipData audioClip;
		private AudioSource audioSource;

		private void OnEnable()
		{
			if (audioSource == null)
				audioSource = GetComponent<AudioSource>();
			CheckState(State.ON_ENABLE);
		}

		private void OnDisable()
		{
			CheckState(State.ON_DISABLE);
		}

		private void CheckState(State currentState)
		{
			if (startAction == currentState)
				PlayClip();
			if (stopAction == currentState)
				StopClip();
		}

		public void Play()
		{
			PlayClip();
		}

		public void Stop()
		{
			StopClip();
		}

		public void PlayOneShot()
		{
			SetAudioClipAndPitch();
			audioSource.PlayOneShot(audioSource.clip);
		}

		public void PlayOneShot(AudioClipData audioClip)
		{
			this.audioClip = audioClip;
			PlayOneShot();
		}

		private void PlayClip()
		{
			SetAudioClipAndPitch();
			audioSource.Play();
		}

		private void SetAudioClipAndPitch()
		{
			audioSource.clip = audioClip.GetAudioClip();
			audioSource.pitch = audioClip.GetPitchOffset();
		}

		private void StopClip()
		{
			audioSource.Stop();
		}
	}
}