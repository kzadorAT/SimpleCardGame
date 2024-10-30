using UnityEngine;
using Unity.Netcode.Components;

[DisallowMultipleComponent] // Para que no se agreguen mas de 1 transform
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
