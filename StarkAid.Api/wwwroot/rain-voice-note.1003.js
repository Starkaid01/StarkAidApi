var final_transcript = '';
var recognizing = false;
var ignore_onend = false;
var externalControl = false; // Flag para controle externo (Windows Forms)
var start_timestamp;
var l = 0;
var i = 0;
var recognition = null;

if (!('webkitSpeechRecognition' in window)) {
    upgrade();
} else {
    rainvn_mic_button.style.display = 'inline-block';
    recognition = new webkitSpeechRecognition();
    recognition.continuous = true;
    recognition.interimResults = true;
    recognition.lang = "pt-BR";
    
    recognition.onstart = function() {
        recognizing = true;
        mic_button_img.src = 'rainvn_load.gif';
    };

    recognition.onend = function() {
        recognizing = false;
        // Se controle externo está ativo, só reinicia se explicitamente solicitado
        if (externalControl && ignore_onend) {
            recognition.lang = "pt-BR";
            recognition.start();
            ignore_onend = true;
            showButtons('none');
            if (event && event.timeStamp) {
                start_timestamp = event.timeStamp;
            }
            return;
        }
        // Se não é controle externo e ignore_onend está true, reinicia
        if (!externalControl && ignore_onend) {
            recognition.lang = "pt-BR";
            recognition.start();
            ignore_onend = true;
            showButtons('none');
            if (event && event.timeStamp) {
                start_timestamp = event.timeStamp;
            }
            return;
        }
        // Para definitivamente
        mic_button_img.src = 'rainvn_mic_button.png';
        if (!final_transcript) {
            return;
        }
        if (window.getSelection) {
            window.getSelection().removeAllRanges();
            var range = document.createRange();
            range.selectNode(document.getElementById('rainvn_text_final'));            
            window.getSelection().addRange(range);
        }
    };

    recognition.onresult = function(event) {
        var interim_transcript = '';
        for (var i = event.resultIndex; i < event.results.length; ++i) {
            if (event.results[i].isFinal) {               
                final_transcript += event.results[i][0].transcript;                
                postWebViewMessage(event.results[i][0].transcript);
                ++l;
                if(l > 15){
                    //alert("limpar");      
                    final_transcript = '';
                    rainvn_text_final.innerHTML = '';
                    rainvn_text_interim.innerHTML = '';
                    showButtons('none');
                    l=0;
                }
            } else {
                interim_transcript += event.results[i][0].transcript;                
            }
        }
        final_transcript = capitalize(final_transcript);
        rainvn_text_final.innerHTML = linebreak(final_transcript);
        rainvn_text_interim.innerHTML = linebreak(interim_transcript);
        if (final_transcript || interim_transcript) {
            showButtons('inline-block');
        }
    };
}

function upgrade() {
    rainvn_mic_button.style.visibility = 'hidden';
}

var two_line = /\n\n/g;
var one_line = /\n/g;

function linebreak(s) {
    return s.replace(two_line, '<p></p>').replace(one_line, '<br>');
}
var first_char = /\S/;

function capitalize(s) {
    return s.replace(first_char, function(m) {
        return m.toUpperCase();
    });
}

function rainvnMicButton(event) {
    if (recognizing) {
        // Parar definitivamente quando clicado manualmente (não reinicia)
        ignore_onend = false;
        externalControl = false;
        recognition.stop();
        return;
    }
    final_transcript = '';
    recognition.lang = "pt-BR";
    ignore_onend = true;
    externalControl = false; // Clique manual não usa controle externo
    recognition.start();
    rainvn_text_final.innerHTML = '';
    rainvn_text_interim.innerHTML = '';
    mic_button_img.src = 'rainvn_mic_button.png';
    showButtons('none');
    if (event && event.timeStamp) {
        start_timestamp = event.timeStamp;
    }
}

// Funções para controle externo (Windows Forms)
function startRecognition() {
    if (!recognition) return;
    if (recognizing) return;
    
    externalControl = true; // Marca que está sob controle externo
    ignore_onend = true; // Permite reinício automático
    final_transcript = '';
    recognition.lang = "pt-BR";
    recognition.start();
    rainvn_text_final.innerHTML = '';
    rainvn_text_interim.innerHTML = '';
    mic_button_img.src = 'rainvn_mic_button.png';
    showButtons('none');
}

function stopRecognition() {
    if (!recognition) return;
    if (!recognizing) return;
    
    // Para definitivamente sem reiniciar
    ignore_onend = false;
    externalControl = false;
    recognition.stop();
}

function isRecognizing() {
    return recognizing;
}

var current_style;

function showButtons(style) {
    if (style == current_style) {
        return;
    }
    current_style = style;      
    
}

function sendMsg(msggf){
    try {
      } catch (error) {
        console.error("Aqui: " + error);
      }      
}

function postWebViewMessage(message){
	try{
		if (window.hasOwnProperty("chrome") && typeof chrome.webview !== undefined) {
			// Windows
			chrome.webview.postMessage(message);
		} else if (window.hasOwnProperty("unoWebView")) {
			// Android
			unoWebView.postMessage(JSON.stringify(message));
		} else if (window.hasOwnProperty("webkit") && typeof webkit.messageHandlers !== undefined) {
			// iOS and macOS
			webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(message));
		}
	}
	catch (ex){
		//alert("Error occurred: " + ex);
	}
}
