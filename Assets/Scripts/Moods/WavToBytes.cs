using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

using System.Net;
using System.Text;

public static class WavToBytes 
{
	private const int HEADER_SIZE = 44;

	public static byte[] GetByteArray(AudioClip clip) {
		var hz = clip.frequency;
		var channels = clip.channels;

		float[] samples = new float[clip.samples];
		clip.GetData(samples, 0);
		Int16[] intData = new Int16[samples.Length];
		Byte[] bytesData = new Byte[samples.Length * 2 + HEADER_SIZE];

		int cpyOffset = 0;

		//------WRITE HEADER-----
		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		Buffer.BlockCopy(riff, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		Byte[] chunkSize = BitConverter.GetBytes(bytesData.Length - 8);
		Buffer.BlockCopy(chunkSize, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		Buffer.BlockCopy(wave, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		Buffer.BlockCopy(fmt, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		Byte[] subChunk1 = BitConverter.GetBytes(16);
		Buffer.BlockCopy(subChunk1, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		UInt16 one = 1;

		Byte[] audioFormat = BitConverter.GetBytes(one);
		Buffer.BlockCopy(audioFormat, 0, bytesData, cpyOffset, 2);
		cpyOffset += 2;

		Byte[] numChannels = BitConverter.GetBytes(channels);
		Buffer.BlockCopy(numChannels, 0, bytesData, cpyOffset, 2);
		cpyOffset += 2;

		Byte[] sampleRate = BitConverter.GetBytes(hz);
		Buffer.BlockCopy(sampleRate, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
		Buffer.BlockCopy(byteRate, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		UInt16 blockAlign = (ushort) (channels * 2);
		Buffer.BlockCopy(BitConverter.GetBytes(blockAlign), 0, bytesData, cpyOffset, 2);
		cpyOffset += 2;

		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		Buffer.BlockCopy(bitsPerSample, 0, bytesData, cpyOffset, 2);
		cpyOffset += 2;

		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		Buffer.BlockCopy(datastring, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		Byte[] subChunk2 = BitConverter.GetBytes(samples.Length * channels * 2);
		Buffer.BlockCopy(subChunk2, 0, bytesData, cpyOffset, 4);
		cpyOffset += 4;

		//------WRITE DATA-------
		const float rescaleFactor = 32767; //to convert float to Int16 (max value for Int16)

		for (int i = 0; i < samples.Length; i++)
		{
			intData[i] = (short)(samples[i] * rescaleFactor);
		}

		Buffer.BlockCopy(intData, 0, bytesData, cpyOffset, samples.Length * channels * 2);

		return bytesData;
	}
}