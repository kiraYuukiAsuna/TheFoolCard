using Godot;
using System.Collections.Generic;

namespace FoolCard
{

public partial class CardDisplay : TextureButton
{
	[Signal] public delegate void ClickedEventHandler(int cardId);

	public int  CardId    { get; private set; } = -1;
	public bool IsFaceDown{ get; private set; }
	public bool IsSelected{ get; private set; }
	public bool Interactable { get => !Disabled; set => Disabled = !value; }

	public CardDisplay()
	{
		CustomMinimumSize = new Vector2(80, 120);
		StretchMode = StretchModeEnum.KeepAspectCentered;
		IgnoreTextureSize = true;
		SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
	}

	public void SetSize(Vector2 size) { CustomMinimumSize = size; Size = size; }

	public void SetCard(int cardId, bool faceDown = false) {
		CardId = cardId;
		IsFaceDown = faceDown;
		UpdateTexture();
	}

	public void SetSelected(bool selected) {
		IsSelected = selected;
		SelfModulate = selected ? new Color(1.5f, 1.5f, 0.5f) : Colors.White; // Boost brightness for highlight
	}

	private void UpdateTexture()
	{
		string path = IsFaceDown ? "res://card/cardback.png" : CardInfo.ResPath(CardId);

		try
		{
			if (ResourceLoader.Exists(path))
			{
				var tex = ResourceLoader.Load<Texture2D>(path);
				if (tex != null)
				{
					TextureNormal = tex;
					return;
				}
			}

			GD.PrintErr($"[CardDisplay] Failed to load: {path}. Using cardback.");
			TextureNormal = ResourceLoader.Load<Texture2D>("res://card/cardback.png");
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"[CardDisplay] Exception loading {path}: {e.Message}");
			TextureNormal = ResourceLoader.Load<Texture2D>("res://card/cardback.png");
		}
	}

	public override void _Pressed() { EmitSignal(SignalName.Clicked, CardId); }

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (!Interactable || IsFaceDown) return default;
		
		var preview = new TextureRect {
			Texture = TextureNormal,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			CustomMinimumSize = CustomMinimumSize,
			Modulate = new Color(1, 1, 1, 0.8f)
		};
		var c = new Control();
		c.AddChild(preview);
		preview.Position = -CustomMinimumSize / 2; // Center on cursor
		SetDragPreview(c);
		
		return CardId;
	}
}

}
