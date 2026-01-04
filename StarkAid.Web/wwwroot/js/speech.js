
window.StarkSpeech = {
    recognition: null,
    dotNetRef: null,
    isListening: false,

    init: function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) {
            console.error("Web Speech API not supported in this browser.");
            return false;
        }

        this.recognition = new SpeechRecognition();
        this.recognition.continuous = true;
        this.recognition.interimResults = true;
        this.recognition.lang = 'pt-BR';

        this.recognition.onresult = (event) => {
            let interimTranscript = '';
            let finalTranscript = '';

            for (let i = event.resultIndex; i < event.results.length; ++i) {
                if (event.results[i].isFinal) {
                    finalTranscript += event.results[i][0].transcript;
                } else {
                    interimTranscript += event.results[i][0].transcript;
                }
            }

            if (finalTranscript) {
                this.dotNetRef.invokeMethodAsync('OnSpeechResult', finalTranscript, false);
            } else if (interimTranscript) {
                this.dotNetRef.invokeMethodAsync('OnSpeechResult', interimTranscript, true);
            }
        };

        this.recognition.onerror = (event) => {
            if (event.error !== 'no-speech') {
                console.error("Speech recognition error:", event.error);
            }
            if (this.isListening) {
                // Restart on error if we should be listening
                setTimeout(() => {
                    try { this.recognition.start(); } catch (e) { }
                }, 100);
            }
        };

        this.recognition.onend = () => {
            if (this.isListening) {
                // Restart if it ends unexpectedly
                try { this.recognition.start(); } catch (e) { }
            }
        };

        return true;
    },

    start: function () {
        if (!this.recognition) return;
        this.isListening = true;
        try {
            this.recognition.start();
        } catch (e) {
            console.warn("Recognition already started or error:", e);
        }
    },

    stop: function () {
        if (!this.recognition) return;
        this.isListening = false;
        this.recognition.stop();
    },

    speak: function (text) {
        return new Promise((resolve, reject) => {
            if (!window.speechSynthesis) {
                reject("Speech synthesis not supported.");
                return;
            }

            // Cancel any ongoing speech
            window.speechSynthesis.cancel();

            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = 'pt-BR';

            // Try to find a good Portuguese voice
            const voices = window.speechSynthesis.getVoices();
            const ptVoice = voices.find(v => v.lang.startsWith('pt')) || voices[0];
            if (ptVoice) utterance.voice = ptVoice;

            utterance.onstart = () => {
                this.dotNetRef.invokeMethodAsync('OnTtsStart');
            };

            utterance.onend = () => {
                this.dotNetRef.invokeMethodAsync('OnTtsEnd');
                resolve();
            };

            utterance.onerror = (e) => {
                // Ignore errors caused by cancellation or interruption
                if (e.error === 'interrupted' || e.error === 'canceled') {
                    console.log("TTS canceled/interrupted");
                    this.dotNetRef.invokeMethodAsync('OnTtsEnd');
                    resolve();
                } else {
                    console.error("TTS Error:", e);
                    this.dotNetRef.invokeMethodAsync('OnTtsEnd');
                    reject(e);
                }
            };

            window.speechSynthesis.speak(utterance);
        });
    },

    cancelSpeak: function () {
        if (window.speechSynthesis) {
            window.speechSynthesis.cancel();
            this.dotNetRef.invokeMethodAsync('OnTtsEnd');
        }
    }
};
