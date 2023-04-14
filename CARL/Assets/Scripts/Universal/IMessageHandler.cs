using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMessageHandler
{
    public bool MsgHandlerSet { get;}
    public IEnumerator RegisterMsgHandlers(); 
}
