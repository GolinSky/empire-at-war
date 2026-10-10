using MPUIKIT;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>Builds the procedural MPImage shapes shared by the hardpoint marker and tooltip.</summary>
    public static class HardPointOverlayGraphics
    {
        public static MPImage CreateImage(string name, RectTransform parent, Vector2 size, Color color,
            float cornerRadius)
        {
            GameObject imageObject = new(name, typeof(RectTransform));
            imageObject.layer = parent.gameObject.layer;
            RectTransform rectTransform = (RectTransform)imageObject.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;

            MPImage image = imageObject.AddComponent<MPImage>();
            image.raycastTarget = false;
            image.color = color;
            image.DrawShape = DrawShape.Rectangle;
            image.FalloffDistance = 1f;

            Rectangle rectangle = image.Rectangle;
            rectangle.CornerRadius = Vector4.one * cornerRadius;
            image.Rectangle = rectangle;
            return image;
        }
    }
}
