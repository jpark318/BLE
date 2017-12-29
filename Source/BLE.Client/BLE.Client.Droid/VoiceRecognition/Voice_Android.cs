using System;
using Android.Media;
using BLE.Client.Interfaces;
using System.IO;
using System.Threading;
using Com.Cloudrail.SI;
using Com.Cloudrail.SI.Interfaces;
using Com.Cloudrail.SI.Services;
using Com.Cloudrail.SI.Types;
//using Amazon.S3;
//using Amazon.S3.Model;

[assembly: Xamarin.Forms.Dependency(typeof(BLE.Client.Droid.VoiceRecognition.Voice_Android))]
namespace BLE.Client.Droid.VoiceRecognition
{
    public class Voice_Android : Voice
    {
        IBusinessCloudStorage service;
        protected static MediaRecorder recorder;
        protected String filePath;

        private const string storage_endpoint = "";
        private string urlParams = "?key=AIzaSyAIwJC2HRnHGZfa4xx84KUpnMB0DaLkSJ8";




        public Voice_Android()
        {
            CloudRail.AppKey = "5a445040fd458621e4248458";

            recorder = new MediaRecorder();
            resetRecorder();
        }

        private void resetRecorder(){
            recorder.Reset();
            recorder.SetAudioSource(AudioSource.Mic);
            recorder.SetAudioSamplingRate(8000);
            recorder.SetAudioChannels(1);
            recorder.SetOutputFormat(OutputFormat.AmrWb);
            recorder.SetAudioEncoder(AudioEncoder.AmrWb);

            DateTime now = DateTime.Now;
            string dateStr = now.ToString();
            dateStr = dateStr.Replace('/', '-');
            dateStr = dateStr.Replace(' ', '-');
            dateStr = dateStr.Replace(':', '-');


            //Java.IO.File DataDirectoryPath = Android.OS.Environment.DataDirectory;
            string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            string dirPath = path + "/aphasia";
            bool exists = Directory.Exists(dirPath);

            filePath = dirPath + "/" + dateStr + ".awb";

            if (!exists)
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch (Exception e)
                {
                    Console.Beep();
                }
            }
            var newFile = new Java.IO.File(dirPath, dateStr);


            recorder.SetOutputFile(filePath);
        }
       
        public void StartRecord(){
            try
            {
                recorder.Prepare();
                recorder.Start();
            }
            catch (Exception e)
            {
                resetRecorder();
                StartRecord();
            }
        }

        public void StopRecord(){
            try
            {
                recorder.Stop();
                uploadFile();
            } catch {
                resetRecorder();
            }
        }

        private async void uploadFile(){
            System.IO.Stream tempStream = System.IO.File.OpenRead(filePath);
            GoogleCloudPlatform googleCloud = new GoogleCloudPlatform(Android.App.Application.Context,
                                                                      "dominic@voice-recognition-156605.iam.gserviceaccount.com",
                                                                      "-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCHWI5LqYOczFUe\nCH5oAHjpKIQIOZ6c3kmPplam0WqlyFKswjnHLiwtJ6XDYLJiCMbWGX5e8AoMUfD7\n1lk5DPmLwAh8fVXVKaUuywZnUfv0CJsVTUYOkHR4iX/zPk8AiMtra6+BqmUoVe9m\ny0dIh8YeXckL+G42kwEULZux0uDfZQQ0MywqSFtOMa3pyl5M354p2lipG9TpKoP8\nzi40+MUiWwp7M9BcwbHJUjOM7k7BeMdiEItfHGFQ4eXLdby8llc0ClafWp8SxCSH\nJ8syYBGD1yYqlbMGhwlPAy3bEsqNmmLJmdVnVpEVJsvonZQKvC4pKzRdY+YDrakA\niQWfJ70XAgMBAAECggEANTe708JWjsvFWCbM1UYCRON6buWBGXtJ/2LPRY6oWYFY\nCLfiEhB0rFiflCAsY+HBlSO3DctimA+MKunQcV9JrAqZC6IYotVaLvkDjpKs9/p+\nSDT3K/je4xplphZE6BfhrF5ORzThy6dml8usPresTfpgeAV6CJlq3i1Ev/oEE7JJ\nN8GOP8Iikraor74cIC/3/xbEQaKyv0V+sTO6Ygh9kpvnqiGwzdsW/PCrSjSS/5V6\n+kAwi3dWJOVzTbQomeW3p72PlAExASscnnuT63zoFMtWuZzzqrbvBlE9NLqzv6xZ\nSXQguki+n7LMVV6zp4xnXamBOcWCN8Wdv9ZG4m/AIQKBgQC+DiiQI1IY8CdCulB1\nnqI5s9yigPvoHedikD+FdAw97Gz4zlHTg/XNzvAFu6/v+ysV9DWRy3Nmcc5Gvuap\nyJFmfGOFefjycuD0qW6M7xIRQTYcAdYoyGZtmPi5HO28MM+YVSewTWUCC5vkx/Sg\nIw9nsW0KPrP4Pa0h/o/16z/tMQKBgQC2TsjkrKoKAspv4+/aA7RlHbJBl5vzztid\nNbwnK+OZ3dOpIAn7+iW4FmbnzQNO3jiG9lAYAmPo6ucSyJK0/jBgxefGZDBwcr6K\ngHefVNnAs+1KhlVoaMy29W4DeIkC7yUf+OhDasZ20pxquiV4co627f5VAQaTr3Pc\nXBZ1xZUcxwKBgQCtvLer+/3iujbJsxQ2UYuvABLjotGlQSDyYzcOQiWmvehoEgOP\nPgSH3XJha0/MK2kZqqMF4lxd/A87cOvfrW/tpiw8KmI/EHFAd1qOD0YO6/QQ6kTi\nB0BLVBma7y0Mafp8IOwlKLr7ga1DGN8xPJuqiPFK+kL+3TLV7qWfgyxvIQKBgAkm\nMQzQ4YO5GiG9ZbciQnZkpCKIkkoNEm/pV3T7zeNV755oPjgIGMaBUU7Gyii2HE4h\npGlgDVWOHGSj7kDpFNJ2fChHtOqfx5I52kcDwh3aqcj4ruabg9KWxJul+/JKwCk3\nm6hufmFONo1gpCrETQc/MGlhsMvOnVjswi/M56vXAoGBAIMkykggG3M0pl0Icyfy\nU/ugsKex2gV0mVxP/oauHbMDxfbnNSkP2BpKdm8HfxAYG4sEJFG+lgw8CnBxyuH6\nXdqNWP31XdxoFVNqtdjhFxr+OiGGRD+RiSxSQYsWbFv/sDD5CThw1513LTDZyH5U\naR3kp8nhjhToha/ffFxC17nN\n-----END PRIVATE KEY-----\n",
                                                                      "voice-recognition-156605");
            service = googleCloud;
            Bucket bucket = new Bucket();
            bucket.Name = "aphasiarogers";
            var filePathArr = filePath.Split('/');
            var keyName = filePathArr[filePathArr.Length - 1];
            keyName = "audio/" + keyName;
            new Thread(() =>
            {
                try
                {
                    service.UploadFile(bucket,
                                       keyName,
                                       tempStream,
                                       tempStream.Length);
                } catch (Exception e){
                    var hi = Console.CapsLock;
                }
            }).Start();
            recorder.Reset();
        }
    }
}
