using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TextWriter : MonoBehaviour
{
    public TMP_Text text;
    public string target;
    public int show;

    public AudioClip snd;
    
    public float autoShow = 0f;
    public float autoHide = 0f;

    public UnityAction<int> OnDelta;
    public UnityAction OnComplete;
    
    float clock;
    
    void Start()
    {
        text ??= GetComponent<TMP_Text>();
        
        if (string.IsNullOrEmpty(target))
        {
            target = text.text;
            show = target.Length;
        }
        
        Update();
    }

    string _oldTarget;
    int _oldShow;
    
    void Update()
    {
        if (_oldShow != show || _oldTarget != target)
        {
            text.text = TextUtils.CutSmart(target, show);
            _oldShow = show;
            _oldTarget = target;
        }

        if (autoShow > 0 || autoHide > 0)
        {
            clock += Time.deltaTime;

            if (autoShow > 0 && clock > autoShow)
            {
                var visibleLength = TextUtils.GetVisibleLength(target);
                
                if (show < visibleLength)
                {
                    show++;
                    clock = 0;
                    OnDelta?.Invoke(1);

                    var charSmart = TextUtils.CharSmart(target, show);
                    gameObject.BroadcastMessage("TextWriterDelta", charSmart);

                    if(charSmart != " ") if (snd != null) AudioSource.PlayClipAtPoint(snd, Vector3.zero);
                    
                    if (show == visibleLength)
                        OnComplete?.Invoke();
                }
            }
            
            if (autoHide < 0 && clock > autoHide)
            {
                if (show > 0)
                {
                    show--;
                    clock = 0;
                    OnDelta?.Invoke(-1);

                    gameObject.BroadcastMessage("TextWriterDelta", TextUtils.CharSmart(target, show));

                    if (show == 0)
                        OnComplete?.Invoke();
                }
            }
        }
    }
}
