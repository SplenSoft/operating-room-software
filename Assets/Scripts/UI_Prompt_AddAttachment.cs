using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton
/// </summary>
public class UI_Prompt_AddAttachment : MonoBehaviour
{
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        AttachmentPoint.AttachmentPointHoverStateChanged += AttachmentPointHoverStateChanged;
        gameObject.SetActive(false);
    }

    private void AttachmentPointHoverStateChanged(object sender, EventArgs e)
    {
        bool active = AttachmentPoint.HoveredAttachmentPoint != null;
        gameObject.SetActive(active);
        if (active)
        {
            SetPosition();
        }
    }

    private void SetPosition()
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(AttachmentPoint.HoveredAttachmentPoint.transform.position);
        _rectTransform.position = screenPos;
    }

    private void Update()
    {
        if (AttachmentPoint.HoveredAttachmentPoint == null)
        {
            gameObject.SetActive(false);
            return;
        }
        SetPosition();
    }
}
