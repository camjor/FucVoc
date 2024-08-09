using NativeWebSocket;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

public class STUNMyChannels : MonoBehaviour {

    [Header("Video Transmission")]
    [SerializeField] private Camera cameraStream;
    [SerializeField] private RawImage sourceImage;
    [SerializeField] private List<RawImage> receiveImages = new List<RawImage>();

    [Header("Audio Transmission")]
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioSource outputAudioSource;

    [Header("WebRTC Transmission Test")]
    //[SerializeField] private bool startDataChannel = false;
    [SerializeField] private bool startVideoAudioChannel = false;
    [SerializeField] private bool sendMessageViaDataChannel = false;

    [Header("Websocket Data")]
    [SerializeField] private bool sendTestMessage = false;

    private RTCPeerConnection connection;
    private RTCDataChannel senderDataChannel;
    private RTCDataChannel receiverDataChannel;

    private WebSocket ws;
    private string clientId;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;

    private bool isRecording = false;
    private string selectedMicrophone;

    private AudioClip recordedClip;
    private int receiveImageCounter = 0;

    //public OVRLipSyncMicInput OLSMicINput;

    //Canvas
    public ManagerCanvas metodoCU;
    public bool conexionSC = false;

    private async void Start() {

        clientId = gameObject.name;

        /*ws = new WebSocket("wss://unity-stun-signaling.glitch.me/", new Dictionary<string, string>() {
            { "user-agent", "unity webrtc datachannel" }
        });*/
        //ws = new WebSocket("wss://192.168.10.18:8005/", new Dictionary<string, string>() {
        ws = new WebSocket($"wss://western-honorable-pleasure.glitch.me/{nameof(VideoChatMediaStreamService)}", new Dictionary<string, string>() {
            { "user-agent", "unity webrtc datachannel" }
        });

        ws.OnOpen += () => {
            // STUN server config
	    //Debug.Log("Conexión abierta");
            RTCConfiguration config = default;
            config.iceServers = new[] {
                new RTCIceServer {
                    urls = new[] {
                        "stun:stun.l.google.com:19302"
                    }
                }
            };

            connection = new RTCPeerConnection(ref config);
            connection.OnIceCandidate = candidate => {
                var candidateInit = new CandidateInit() {
                    SdpMid = candidate.SdpMid,
                    SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                    Candidate = candidate.Candidate
                };

                ws.SendText("CANDIDATE!" + candidateInit.ConvertToJSON());
            };
            connection.OnIceConnectionChange = state => {
                Debug.Log(state);
               
               switch (state)
                {
                    case RTCIceConnectionState.Connected:
                        // Acción cuando la conexión está establecida
                        //Debug.Log("Conexión establecida");
                        conexionSC=true;
                        break;
                    case RTCIceConnectionState.Completed:
                        // Acción cuando la conexión está establecida
                        //Debug.Log("Conexión completa");
                        conexionSC=true;
                        break;
                        
                    case RTCIceConnectionState.Disconnected:
                        // Acción cuando la conexión se desconecta
                        //Debug.Log("Conexión desconectada");
                        conexionSC=false;
                        //connection.Close();
                        //OnDestroy();
                        metodoCU.conexionUser();
                        break;
                    case RTCIceConnectionState.Failed:
                        // Acción cuando la conexión falla
                        //Debug.Log("Conexión fallida");
                        conexionSC=false;
                        //metodoCU.conexionUser();
                        break;
                    // Agrega más casos según tus necesidades
                    default:
                        // Acción por defecto (si no coincide con ningún caso)
                        Debug.Log("Estado desconocido");
                        conexionSC=false;
                        break;
                }
            };

            senderDataChannel = connection.CreateDataChannel("sendChannel");
            senderDataChannel.OnOpen = () => {
                Debug.Log("Sender opened channel");
                
            };
            senderDataChannel.OnClose = () => {
                Debug.Log("Sender closed channel");
            };

            connection.OnDataChannel = channel => {
                receiverDataChannel = channel;
                receiverDataChannel.OnMessage = bytes => {
                    var message = Encoding.UTF8.GetString(bytes);
                    Debug.Log("Receiver received: " + message);
                };
            };

            connection.OnTrack = e => {
                if (e.Track is VideoStreamTrack video) {
                    video.OnVideoReceived += tex => {
                    receiveImages[receiveImageCounter].texture = tex;
                    receiveImageCounter++;
                    };
                }
                if (e.Track is AudioStreamTrack audio) {
                    outputAudioSource.SetTrack(audio);
                    outputAudioSource.loop = true;
                    outputAudioSource.Play();
                }
            };

            connection.OnNegotiationNeeded = () => {
                StartCoroutine(CreateOffer());
            };

            StartCoroutine(WebRTC.Update());
        };

        ws.OnMessage += (bytes) => {
            var data = Encoding.UTF8.GetString(bytes);
            var signalingMessage = new SignalingMessage(data);

            switch (signalingMessage.Type) {
                case SignalingMessageType.OFFER:
                    Debug.Log(clientId + " - Got OFFER: " + signalingMessage.Message);
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedOffer = true;
                    break;
                case SignalingMessageType.ANSWER:
                    Debug.Log(clientId + " - Got ANSWER: " + signalingMessage.Message);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedAnswer = true;
                    break;
                case SignalingMessageType.CANDIDATE:
                    Debug.Log(clientId + " - Got CANDIDATE: " + signalingMessage.Message);

                    // generate candidate data
                    var candidateInit = CandidateInit.FromJSON(signalingMessage.Message);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    // add candidate to this connection
                    connection.AddIceCandidate(candidate);
                    break;
                default:
                    Debug.Log(clientId + " - Received: " + data);
                    break;
            }
        };
        selectedMicrophone = Microphone.devices[0];
        Debug.Log("el microfono es " + selectedMicrophone);

        await ws.Connect();
    }

    private void Update() {
        if (hasReceivedOffer) {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
        if (hasReceivedAnswer) {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }
        //if (startDataChannel) {
        //    startDataChannel = !startDataChannel;

        //    senderDataChannel = connection.CreateDataChannel("sendChannel");
        //    senderDataChannel.OnOpen = () => {
        //        Debug.Log("Sender opened channel");
        //    };
        //    senderDataChannel.OnClose = () => {
        //        Debug.Log("Sender closed channel");
        //    };
        //}
        if (sendMessageViaDataChannel) {
            sendMessageViaDataChannel = !sendMessageViaDataChannel;
            senderDataChannel.Send("TEST!WEBRTC DATACHANNEL TEST");
        }
        if (startVideoAudioChannel) {
            startVideoAudioChannel = !startVideoAudioChannel;

            // video
            var videoStreamTrack = cameraStream.CaptureStreamTrack(1280, 720);
            sourceImage.texture = cameraStream.targetTexture;
            connection.AddTrack(videoStreamTrack);

            // audio

            //inputAudioSource.loop = true;
            //inputAudioSource.clip = OLSMicINput.recordedClipLS;
            inputAudioSource.clip=recordedClip;
            Debug.Log("inputaudiosource "+ inputAudioSource);
            inputAudioSource.Play();
            var audioStreamTrack = new AudioStreamTrack(inputAudioSource);
            //audioStreamTrack.Loopback = true;//retorno
            connection.AddTrack(audioStreamTrack);
        }
        if (sendTestMessage) {
            sendTestMessage = !sendTestMessage;
            ws.SendText("TEST!WEBSOCKET TEST");
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        ws.DispatchMessageQueue();
#endif
    }

    private void OnDestroy() {
        if (senderDataChannel != null) {
            senderDataChannel.Close();
        }
        Debug.Log("se cerro");
        connection.Close();
        ws.Close();
    }

    private IEnumerator CreateOffer() {
        var offer = connection.CreateOffer();
        yield return offer;

        var offerDesc = offer.Desc;
        var localDescOp = connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        // send desc to server for receiver connection
        var offerSessionDesc = new SessionDescription() {
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp
        };
        ws.SendText("OFFER!" + offerSessionDesc.ConvertToJSON());
    }

    private IEnumerator CreateAnswer() {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        // send desc to server for sender connection
        var answerSessionDesc = new SessionDescription() {
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp
        };
        ws.SendText("ANSWER!" + answerSessionDesc.ConvertToJSON());
    }

    private IEnumerator SetRemoteDesc() {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }

    //public void StartDataChannel() {
    //    startDataChannel = true;
    //}

    /*public void ReiniciarConexion()
    {
        // Cierra la conexión actual
        connection.Close();

        // Crea una nueva oferta (SDP offer)
        connection.CreateOffer().Then(offer =>
        {
            // Establece la oferta y espera la respuesta (SDP answer)
            peerConnection.SetLocalDescription(ref offer);
            // Envía la oferta al otro usuario
            // ...
        });
    }*/

    public void StartVideoAudio() {
        startVideoAudioChannel = true;
    }

    public void SendWebSocketTestMessage() {
        sendTestMessage = true;
    }

    public void SendWebRTCDataChannelTestMessage() {
        sendMessageViaDataChannel = true;
    }
      public void ToggleRecording()
    {
        if (!isRecording)
        {
            // Comenzar la grabación
            recordedClip = Microphone.Start(selectedMicrophone, true, 1, 44100);
            isRecording = true;
            Debug.Log("Grabando");
        }
        else
        {
            // Detener la grabación
            Microphone.End(selectedMicrophone);
            isRecording = false;
            Debug.Log("Corte");
        }
    }
}