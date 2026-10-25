/**
 * NavigationAudio.jslib
 *
 * WebGL JavaScript plugin for SmartClassroomAR.
 * Provides a bridge between Unity C# (NavigationAudioController) and
 * the browser's Web Speech API (SpeechSynthesis).
 *
 * Handles:
 *   - Browser autoplay / user-gesture restrictions.
 *   - Speech completion callback into Unity via SendMessage for floor synchronization.
 *   - Graceful fallback when speechSynthesis is unavailable.
 *   - Cancels pending speech before starting a new utterance to prevent overlap.
 *
 * Deployed to: Assets/Plugins/WebGL/NavigationAudio.jslib
 */
var NavigationAudioPlugin = {
    NavAudioBridge_SpeakWithCallback: function(textPtr, volume, gameObjectNamePtr, callbackMethodNamePtr) {
        var text = UTF8ToString(textPtr);
        var goName = UTF8ToString(gameObjectNamePtr);
        var cbMethod = UTF8ToString(callbackMethodNamePtr);

        function notifyComplete(status) {
            if (goName && cbMethod && typeof SendMessage === 'function') {
                try {
                    SendMessage(goName, cbMethod, status || 'completed');
                } catch (e) {
                    console.warn('[NavigationAudio] SendMessage callback failed:', e);
                }
            }
        }

        if (!text || text.trim().length === 0) {
            notifyComplete('empty');
            return;
        }

        if (!window.speechSynthesis) {
            console.warn('[NavigationAudio] SpeechSynthesis not supported in this browser.');
            notifyComplete('unsupported');
            return;
        }

        try {
            window.speechSynthesis.cancel();
        } catch (e) {}

        var utterance = new SpeechSynthesisUtterance(text);
        utterance.lang   = 'en-IN';
        utterance.rate   = 0.90;
        utterance.pitch  = 1.0;
        utterance.volume = Math.max(0, Math.min(1, volume));

        var finished = false;
        function finishOnce(status) {
            if (finished) return;
            finished = true;
            notifyComplete(status);
        }

        utterance.onend = function() {
            finishOnce('completed');
        };

        utterance.onerror = function(e) {
            if (e.error !== 'interrupted') {
                console.warn('[NavigationAudio] SpeechSynthesis error:', e.error);
            }
            finishOnce('error');
        };

        var voices = window.speechSynthesis.getVoices();
        var preferred = voices.find(function(v) {
            return v.lang.startsWith('en') && v.name.toLowerCase().indexOf('female') !== -1;
        }) || voices.find(function(v) {
            return v.lang.startsWith('en');
        });
        if (preferred) utterance.voice = preferred;

        window.speechSynthesis.speak(utterance);
    },

    NavAudioBridge_Speak: function(textPtr, volume) {
        var text = UTF8ToString(textPtr);
        if (!text || text.trim().length === 0) return;

        if (!window.speechSynthesis) {
            return;
        }

        try {
            window.speechSynthesis.cancel();
        } catch (e) {}

        var utterance = new SpeechSynthesisUtterance(text);
        utterance.lang   = 'en-IN';
        utterance.rate   = 0.90;
        utterance.pitch  = 1.0;
        utterance.volume = Math.max(0, Math.min(1, volume));

        var voices = window.speechSynthesis.getVoices();
        var preferred = voices.find(function(v) {
            return v.lang.startsWith('en') && v.name.toLowerCase().indexOf('female') !== -1;
        }) || voices.find(function(v) {
            return v.lang.startsWith('en');
        });
        if (preferred) utterance.voice = preferred;

        utterance.onerror = function(e) {};

        window.speechSynthesis.speak(utterance);
    }
};

mergeInto(LibraryManager.library, NavigationAudioPlugin);
