using System;
using System.Threading;
using Android.Media;
using Android.Runtime;
using Google.Cloud.Speech.V1;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using System.Collections.Generic;

[assembly: Xamarin.Forms.Dependency(typeof(BLE.Client.Droid.VoiceRecognition.VoiceRecorder))]
namespace BLE.Client.Droid.VoiceRecognition
{
    public class VoiceRecorder
    {
        protected static Callback mCallBack;
        protected static AudioRecord audioRecorder;
        protected static Java.Lang.Thread audioThread;
        protected static byte[] mBuffer;
        protected static Object thisLock = new Object();

        private ChannelIn CHANNEL = ChannelIn.Mono;
        private Encoding ENCODING = Encoding.Pcm16bit;

        public VoiceRecorder()
        {
            mCallBack = new Callback();
        }

        private AudioRecord CreateAudioRecord()
        {
            var sampleRate = 8000;
            var sizeInBytes = AudioRecord.GetMinBufferSize(sampleRate, CHANNEL, ENCODING);
            AudioRecord audioRecord = new AudioRecord(AudioSource.Default, sampleRate, CHANNEL, ENCODING, sizeInBytes);
            mBuffer = new byte[sizeInBytes];
            return audioRecord;
        }

        public void Start()
        {
            Stop();

            audioRecorder = CreateAudioRecord();
            audioRecorder.StartRecording();
            audioThread = new Java.Lang.Thread(new ProcessVoice());
            audioThread.Start();
        }

        public void Stop()
        {
            lock (thisLock)
            {

            }
        }


        public class Callback {
            protected SpeechClient speech;

            public Callback(){
                try
                {
                    var contents = "{\"type\": \"service_account\", \"project_id\": \"voice-recognition-156605\", \"private_key_id\": \"ec7799374ef85b33379de5db88fd4c743355c378\", \"private_key\": \"-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDRNsnUyi+0tsX3\nW6dh6+dbEeZWyRZ38K23WUHLXBpVuPT28GEsk04S8pxjPSKZCzrRNM96SNXXWTEA\nmmLKV0lJf0zB1hO1cZ4owIVtD1Jp8GWr83OJryMVMW2/A/sr4XMXno7txa0oMXLw\n7WIjcAzjqbwGK5fuzlFHF2f9N94o0llj3G5hCAGOHoCcg80s0abBiE5GORFisLEE\n0rHiErBcywytoKm2bvPBPhBxh4McislXR+Rft0YeDdfy6gow0N5HKGlo/EnZ200H\nhH8wsSGb3L7SchqGxNXI9wersE66l4Xjz1TQIAkIOjQlJA32fzSstpeTmRPm22Sj\nN6F2HexdAgMBAAECggEAA1Yp/e0XAL0s7j7z38vV4IwUyKZ82VRz5MtRwyqYrQjP\nojeRCSzRPYaegO85I8hRQrzUSwlUgGfN4h+NF0oUlQ1aU0fYY8nEun18pQqYmSHf\njaznM7r2EU1gQTjtZJSt6RRT+bho4PiG60gDrtvVwh9orFZoXib868vbrti8xxk/\npoarhPWvjef7vxCnooo6F5kukyHNwQPO8yMMT06/X2DX8y5MYA+rmpTj4BXMcVWe\ntWdbky7b5AfcuucI76Tfi41DnljiMSiX8yQ4ApoOL+XSfq26KAxo9ev7S1Fo09Ds\nX3tU6e+jJcGTEQwlevm4BbOtmR6jZKbufotTI/5F4QKBgQD2LwlN3G5dV6Pjcjnn\nFeGqMA69iW//s4P2O7a4RhSVbr6VQxZjC/zKeV339f7qiOEHTfi03Yx8ubfuHkQC\ny8MKeTe+nHR0ckO5UPFLH52IjLceIwvNnaGs74mCJNiEyX9BpUFBzscOMbg8BOnx\nSGkdUDL6roW/TMlv5nBnIMUWfQKBgQDZjmCOdARhTckm0WA6GAhb3J8uWSDkFwmF\n0bPpvOGBmYrYVarDYijtbAgEKMy6PFt1RaXlbWsttFM5K8IsmA2Vbv6e2grF+++5\ntxB9MDpXgQ0NVP/5Nohf90Mi81FSBS+LCDf+Pjh/0EvJqweu/UXDyp2f3rJxlsk8\nrZUZ4kOzYQKBgENfBe3P6EgVJt2kseHipBoeArqt9P+GEhP9rXhqfVGTuAZDEMpU\nSn7ijevA310xzltgZDKi+sJbVNGOaNBXEO451B6O1HPVnWEGnLIRWdw3nhlaP+2q\nOMeJ2hjKmpJkTjYZ0mz++IyS4LdUJO2KAnIqM3lU73c1vV6pMpOWbTlFAoGAQEs7\nbd4LjVYXpEksTv7bOYqx4Fimx8GnJs0ahnEzk8F0rwpiNOvFfKT4mYIVPtSnkrjK\nlksH6bHpBnRQJi2plgf/Z6K4nFogNppLXTPrigCxgscj/tqG4xWH2cRevAacTlJX\neeOZfuxn+Wl6E9T10S8H9j8yLS+KuUvzTHr51wECgYEA1IAH8kW3/NeVvuhXM0TL\nHk2HfB8wKWq6QJT/ZiZUk5+Z40Glcp/+TrCF6jpRHRz1DmeIfVWf8p1ccg9Dg2CM\n0xiJWbAD5pbzx3H2z9N2thG1fPM5PM17zDKr9wJZ2HA4itDvAr05VK3xUbG9nk8A\nNNem+74TjUc5DizVV2QIpAE=\n-----END PRIVATE KEY-----\n\", \"client_email\": \"dominic@voice-recognition-156605.iam.gserviceaccount.com\", \"client_id\": \"106255852102323868382\", \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\", \"token_uri\": \"https://accounts.google.com/o/oauth2/token\", \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\", \"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/dominic%40voice-recognition-156605.iam.gserviceaccount.com\"}";
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(contents);

                    GoogleCredential googleCredential;
                    using (System.IO.Stream m = new MemoryStream(byteArray))
                          googleCredential = GoogleCredential.FromStream(m);
                    speech = SpeechClient.Create();
                }  catch (Exception e)
                {
                    if (e.InnerException != null)
                    {
                        string err = e.InnerException.Message;
                    }
                }
                //initializeSpeech();
            }

            private void initializeSpeech(){
                var contents = "{\"type\": \"service_account\", \"project_id\": \"voice-recognition-156605\", \"private_key_id\": \"ec7799374ef85b33379de5db88fd4c743355c378\", \"private_key\": \"-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDRNsnUyi+0tsX3\nW6dh6+dbEeZWyRZ38K23WUHLXBpVuPT28GEsk04S8pxjPSKZCzrRNM96SNXXWTEA\nmmLKV0lJf0zB1hO1cZ4owIVtD1Jp8GWr83OJryMVMW2/A/sr4XMXno7txa0oMXLw\n7WIjcAzjqbwGK5fuzlFHF2f9N94o0llj3G5hCAGOHoCcg80s0abBiE5GORFisLEE\n0rHiErBcywytoKm2bvPBPhBxh4McislXR+Rft0YeDdfy6gow0N5HKGlo/EnZ200H\nhH8wsSGb3L7SchqGxNXI9wersE66l4Xjz1TQIAkIOjQlJA32fzSstpeTmRPm22Sj\nN6F2HexdAgMBAAECggEAA1Yp/e0XAL0s7j7z38vV4IwUyKZ82VRz5MtRwyqYrQjP\nojeRCSzRPYaegO85I8hRQrzUSwlUgGfN4h+NF0oUlQ1aU0fYY8nEun18pQqYmSHf\njaznM7r2EU1gQTjtZJSt6RRT+bho4PiG60gDrtvVwh9orFZoXib868vbrti8xxk/\npoarhPWvjef7vxCnooo6F5kukyHNwQPO8yMMT06/X2DX8y5MYA+rmpTj4BXMcVWe\ntWdbky7b5AfcuucI76Tfi41DnljiMSiX8yQ4ApoOL+XSfq26KAxo9ev7S1Fo09Ds\nX3tU6e+jJcGTEQwlevm4BbOtmR6jZKbufotTI/5F4QKBgQD2LwlN3G5dV6Pjcjnn\nFeGqMA69iW//s4P2O7a4RhSVbr6VQxZjC/zKeV339f7qiOEHTfi03Yx8ubfuHkQC\ny8MKeTe+nHR0ckO5UPFLH52IjLceIwvNnaGs74mCJNiEyX9BpUFBzscOMbg8BOnx\nSGkdUDL6roW/TMlv5nBnIMUWfQKBgQDZjmCOdARhTckm0WA6GAhb3J8uWSDkFwmF\n0bPpvOGBmYrYVarDYijtbAgEKMy6PFt1RaXlbWsttFM5K8IsmA2Vbv6e2grF+++5\ntxB9MDpXgQ0NVP/5Nohf90Mi81FSBS+LCDf+Pjh/0EvJqweu/UXDyp2f3rJxlsk8\nrZUZ4kOzYQKBgENfBe3P6EgVJt2kseHipBoeArqt9P+GEhP9rXhqfVGTuAZDEMpU\nSn7ijevA310xzltgZDKi+sJbVNGOaNBXEO451B6O1HPVnWEGnLIRWdw3nhlaP+2q\nOMeJ2hjKmpJkTjYZ0mz++IyS4LdUJO2KAnIqM3lU73c1vV6pMpOWbTlFAoGAQEs7\nbd4LjVYXpEksTv7bOYqx4Fimx8GnJs0ahnEzk8F0rwpiNOvFfKT4mYIVPtSnkrjK\nlksH6bHpBnRQJi2plgf/Z6K4nFogNppLXTPrigCxgscj/tqG4xWH2cRevAacTlJX\neeOZfuxn+Wl6E9T10S8H9j8yLS+KuUvzTHr51wECgYEA1IAH8kW3/NeVvuhXM0TL\nHk2HfB8wKWq6QJT/ZiZUk5+Z40Glcp/+TrCF6jpRHRz1DmeIfVWf8p1ccg9Dg2CM\n0xiJWbAD5pbzx3H2z9N2thG1fPM5PM17zDKr9wJZ2HA4itDvAr05VK3xUbG9nk8A\nNNem+74TjUc5DizVV2QIpAE=\n-----END PRIVATE KEY-----\n\", \"client_email\": \"dominic@voice-recognition-156605.iam.gserviceaccount.com\", \"client_id\": \"106255852102323868382\", \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\", \"token_uri\": \"https://accounts.google.com/o/oauth2/token\", \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\", \"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/dominic%40voice-recognition-156605.iam.gserviceaccount.com\"}";
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(contents);

                GoogleCredential googleCredential;
                try
                {
                    using (System.IO.Stream m = new MemoryStream(byteArray))
                        googleCredential = GoogleCredential.FromStream(m);
                    List<String> scope = new List<string>();
                    scope.Add("https://www.googleapis.com/auth/cloud-platform");
                    GoogleCredential scoped = googleCredential.CreateScoped(scope);
                    Grpc.Core.ChannelCredentials cred = scoped.ToChannelCredentials();
                    var channel = new Grpc.Core.Channel(SpeechClient.DefaultEndpoint.Host,
                                                        SpeechClient.DefaultEndpoint.Port,
                                                        cred);
                    speech = SpeechClient.Create(channel);
                } catch (Exception e){
                    if (e.InnerException != null)
                    {
                        string err = e.InnerException.Message;
                    }
                }
            }

            /**
            * Called when the recorder starts hearing voice.
            */
            public void OnVoiceStart()
            {   
               
            }

            /**
             * Called when the recorder is hearing voice.
             *
             * @param data The audio data in {@link AudioFormat#ENCODING_PCM_16BIT}.
             * @param size The size of the actual data in {@code data}.
             */
            public void OnVoice(byte[] data, int size)
            {
                var streamingCall = speech.StreamingRecognize();
                object writeLock = new object();
                bool writeMore = true;

                lock (writeLock)
                {
                    if (!writeMore) return;
                    streamingCall.WriteAsync(
                        new StreamingRecognizeRequest()
                        {
                            AudioContent = Google.Protobuf.ByteString
                                .CopyFrom(data, 0, size)
                        }).Wait();
                }
            }

            /**
             * Called when the recorder stops hearing voice.
             */
            public void OnVoiceEnd()
            {
            }
        }

        private class ProcessVoice : Java.Lang.Object, Java.Lang.IRunnable
        {
            //IntPtr IJavaObject.Handle => throw new NotImplementedException();
            private long mLastVoiceHeardMillis = long.MaxValue;
            private long mVoiceStartedMillis;
            protected int SPEECH_TIMEOUT_MILLIS = 2000;
            protected int MAX_SPEECH_LENGTH_MILLIS = 30 * 1000;


            public void Run()
            {
                while(true){
                    lock (VoiceRecorder.thisLock)
                    {
                        var size = VoiceRecorder.audioRecorder.Read(VoiceRecorder.mBuffer, 0, VoiceRecorder.mBuffer.Length);
                        long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        if (IsHearingVoice(VoiceRecorder.mBuffer, VoiceRecorder.mBuffer.Length)){
                            if (mLastVoiceHeardMillis == long.MaxValue){
                                mVoiceStartedMillis = now;
                                VoiceRecorder.mCallBack.OnVoiceStart();
                             }

                            VoiceRecorder.mCallBack.OnVoice(VoiceRecorder.mBuffer, size);
                            mLastVoiceHeardMillis = now;
                            if (now - mVoiceStartedMillis > MAX_SPEECH_LENGTH_MILLIS)
                            {
                                End();
                            }
                        } else if (mLastVoiceHeardMillis != long.MaxValue){
                            VoiceRecorder.mCallBack.OnVoice(mBuffer, size);
                            if (now - mLastVoiceHeardMillis > SPEECH_TIMEOUT_MILLIS)
                            {
                                End();
                            }
                        }
                    }
                }
            }

            public new void Dispose(){
                
            }

            public new void Handle(){
                
            }

            private void End(){
                mLastVoiceHeardMillis = long.MaxValue;
                VoiceRecorder.mCallBack.OnVoiceEnd();
            }

            private bool IsHearingVoice(byte[] buffer, int size)
            {
                for (int i = 0; i < size - 1; i += 2)
                {
                    int s = buffer[i + 1];
                    if (s < 0) s *= -1;
                    s <<= 8;
                    s += Math.Abs(buffer[i]);

                    if (s > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
