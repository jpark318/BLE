using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;
using BLE.Client.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(BLE.Client.Droid.VoiceRecognition.Voice_Android))]
namespace BLE.Client.Droid.VoiceRecognition
{
    public class Voice_Android : Voice
    {
        protected VoiceRecorder recorder;
        //protected static MediaRecorder recorder;

        //protected class CallBackImplementation : VoiceRecorder.Callback {
        //    public new void OnVoiceStart()
        //    {
        //        showStatus(true);
        //        if (mSpeechService != null)
        //        {
        //            mSpeechService.startRecognizing(mVoiceRecorder.getSampleRate());
        //        }
        //    }

        //    public new void OnVoice(byte[] data, int size)
        //    {
        //        if (mSpeechService != null)
        //        {
        //            mSpeechService.recognize(data, size);
        //        }
        //    }

        //    /**
        //     * Called when the recorder stops hearing voice.
        //     */
        //    public new void OnVoiceEnd()
        //    {
        //        showStatus(false);
        //        if (mSpeechService != null)
        //        {
        //            mSpeechService.finishRecognizing();
        //        }
        //    } 
        //}

        //private VoiceRecorder.Callback mVoiceCallBack;


        public Voice_Android()
        {
            recorder = new VoiceRecorder();
        }

        //void BeginRecordAudio(String filePath)
        //{
        //    try
        //    {
        //        if (File.Exists(filePath))
        //        {
        //            File.Delete(filePath);
        //        }
        //        if (recorder == null)
        //        {
        //            recorder = new MediaRecorder(); // Initial state.
        //        }
        //        else
        //        {
        //            recorder.Reset();
        //            recorder.SetAudioSource(AudioSource.Mic);
        //            recorder.SetOutputFormat(OutputFormat.ThreeGpp);
        //            recorder.SetAudioEncoder(AudioEncoder.Default);
        //            // Initialized state.
        //            recorder.SetOutputFile(filePath);
        //            // DataSourceConfigured state.
        //            recorder.Prepare(); // Prepared state
        //            recorder.Start(); // Recording state.
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Out.WriteLine(ex.StackTrace);
        //    }
        //}

        //public void StopRecordAudio()
        //{
        //    recorder.Stop();
        //    recorder.Reset();
        //    recorder.Release();
        //}
        public void StartRecord(){
            recorder.Start();    
        }

        public void StopRecord(){
            recorder.Stop();
        }

    }
}
