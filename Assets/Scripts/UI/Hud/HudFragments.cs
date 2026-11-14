using TMPro;
using UnityEngine;

/// <summary>
/// Star Fragment counter: an icon and "x 3".
/// Listens to my teammate's PlayerStarFragments.FragmentCollected(id, count). Hidden if Pip has no
/// PlayerStarFragments.
///
/// Art: the icon is a plain Image (Placeholder_UI_Fragment for now).
/// </summary>
public class HudFragments : PlayerHudElement
{
    [SerializeField] private TMP_Text countText;

    public string ShownText => countText.text;

    private PlayerStarFragments _fragments;

    protected override bool Bind(PlayerController pip)
    {
        _fragments = pip.GetComponent<PlayerStarFragments>();
        if (_fragments == null)
            return false;

        _fragments.FragmentCollected += OnFragmentCollected;
        Show();
        return true;
    }

    protected override void Unbind()
    {
        if (_fragments != null)
            _fragments.FragmentCollected -= OnFragmentCollected;
        _fragments = null;
    }

    protected override void Update()
    {
        base.Update();
        // Fragments loaded from a save don't fire FragmentCollected, so also check the count.
        if (_fragments != null && countText.text != Label(_fragments.FragmentCount))
            Show();
    }

    private void OnFragmentCollected(string id, int count) => Show();

    private void Show() => countText.text = Label(_fragments.FragmentCount);

    private static string Label(int count) => $"x {count}";
}
