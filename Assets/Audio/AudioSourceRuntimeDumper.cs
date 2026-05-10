using System.Text;
using UnityEngine;

public static class AudioSourceRuntimeDumper
{
    public static void DumpAllPlayingAudioSources(string context)
    {
        var sources = GameObject.FindObjectsOfType<AudioSource>(true);

        foreach (var source in sources)
        {
            if (source == null)
                continue;

            string path = GetHierarchyPath(source.transform);
            string group = source.outputAudioMixerGroup ? source.outputAudioMixerGroup.name : "NULL";
            string mixer = source.outputAudioMixerGroup && source.outputAudioMixerGroup.audioMixer
                ? source.outputAudioMixerGroup.audioMixer.name
                : "NULL";
            string clip = source.clip ? source.clip.name : "NULL";

            Debug.Log(
                $"{OptionManager.AudioOptionsTracePrefix} DumpAudioSource {context} " +
                $"path={path} " +
                $"enabled={source.enabled} " +
                $"activeInHierarchy={source.gameObject.activeInHierarchy} " +
                $"isPlaying={source.isPlaying} " +
                $"playOnAwake={source.playOnAwake} " +
                $"volume={source.volume} " +
                $"mute={source.mute} " +
                $"loop={source.loop} " +
                $"group={group} " +
                $"mixer={mixer} " +
                $"clip={clip}");
        }
    }

    public static void DumpAudioSourcesNextFrame(MonoBehaviour owner, string context)
    {
        if (owner == null)
            return;

        owner.StartCoroutine(DumpAudioSourcesNextFrameCoroutine(context));
    }

    public static string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return "NULL";

        var builder = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            builder.Insert(0, t.name + "/");
        }

        return builder.ToString();
    }

    private static System.Collections.IEnumerator DumpAudioSourcesNextFrameCoroutine(string context)
    {
        yield return null;
        DumpAllPlayingAudioSources(context);
    }
}
