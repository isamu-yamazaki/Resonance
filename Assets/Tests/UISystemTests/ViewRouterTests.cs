using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Assemblies.UISystem;
using System.Linq;

public class ViewRouterTests
{
    private ViewRouter router;

    private class MockOverlayView : IOverlayView
    {
        public bool IsShown { get; private set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }

        public void Show() { IsShown = true; ShowCallCount++; }
        public void Hide() { IsShown = false; HideCallCount++; }
    }

    [SetUp]
    public void SetUp()
    {
        router = new ViewRouter();
    }

    #region RegisterOverlay

    [Test]
    public void RegisterOverlay_Once_AddsToOverlayDictionary()
    {
        var view = new MockOverlayView();
        var options = new OverlayOptions { view = view };

        int id = router.RegisterOverlay(options);

        Assert.IsTrue(router.Overlays.ContainsKey(id));
        Assert.AreEqual(view, router.Overlays[id].view);
    }

    [Test]
    public void RegisterOverlay_MultipleTimes_AssignsUniqueIds()
    {
        int id1 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });
        int id2 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });
        int id3 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });

        Assert.AreNotEqual(id1, id2);
        Assert.AreNotEqual(id2, id3);
        Assert.AreNotEqual(id1, id3);
    }

    #endregion

    #region ShowOverlay

    [Test]
    public void ShowOverlay_UnregisteredId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => router.ShowOverlay(999));
    }

    [Test]
    public void ShowOverlay_AlreadyShownOverlay_IsNoOp()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });
        router.ShowOverlay(id);

        router.ShowOverlay(id);

        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void ShowOverlay_RegisteredOverlay_AddsToActiveOverlayIds()
    {
        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });

        router.ShowOverlay(id);

        Assert.IsTrue(router.ActiveOverlayIds.Contains(id));
    }

    [Test]
    public void ShowOverlay_RegisteredOverlay_CallsViewShow()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });

        router.ShowOverlay(id);

        Assert.IsTrue(view.IsShown);
        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void ShowOverlay_WithUnlockCursor_UnlocksCursor()
    {
        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });

        router.ShowOverlay(id);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void ShowOverlay_WithoutUnlockCursor_LocksCursor()
    {
        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = false });

        router.ShowOverlay(id);

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsFalse(Cursor.visible);
    }

    [Test]
    public void ShowOverlay_WithoutUnlockCursor_WhenOtherActiveOverlayUnlocks_KeepsCursorUnlocked()
    {
        int unlockingId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        int lockingId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = false });
        router.ShowOverlay(unlockingId);

        router.ShowOverlay(lockingId);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void ShowOverlay_WithInputMaps_DisablesInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        inputMap.Enable();
        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { inputMap } });

        router.ShowOverlay(id);

        Assert.IsFalse(inputMap.enabled);
    }

    [Test]
    public void ShowOverlay_InactiveOverlayWithUniqueInputMap_ReenablesIt()
    {
        var activeMap = new InputActionMap("activeMap");
        activeMap.AddAction("action");
        var inactiveMap = new InputActionMap("inactiveMap");
        inactiveMap.AddAction("action");
        inactiveMap.Disable();

        int activeId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { activeMap } });
        router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { inactiveMap } });

        router.ShowOverlay(activeId);

        Assert.IsTrue(inactiveMap.enabled);
    }

    [Test]
    public void ShowOverlay_SharedInputMap_NotReenabledWhenAlsoInActiveOverlay()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        int activeId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { sharedMap } });

        router.ShowOverlay(activeId);

        Assert.IsFalse(sharedMap.enabled);
    }

    #endregion

    #region HideOverlay

    [Test]
    public void HideOverlay_NonActiveOverlay_IsNoOp()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });

        router.HideOverlay(id);

        Assert.AreEqual(0, view.HideCallCount);
        Assert.IsFalse(router.ActiveOverlayIds.Contains(id));
    }

    [Test]
    public void HideOverlay_ActiveOverlay_RemovesFromActiveOverlayIds()
    {
        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });
        router.ShowOverlay(id);

        router.HideOverlay(id);

        Assert.IsFalse(router.ActiveOverlayIds.Contains(id));
    }

    [Test]
    public void HideOverlay_ActiveOverlay_CallsViewHide()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });
        router.ShowOverlay(id);

        router.HideOverlay(id);

        Assert.IsFalse(view.IsShown);
        Assert.AreEqual(1, view.HideCallCount);
    }

    [Test]
    public void HideOverlay_LastUnlockingOverlay_LocksCursor()
    {
        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        router.ShowOverlay(id);

        router.HideOverlay(id);

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsFalse(Cursor.visible);
    }

    [Test]
    public void HideOverlay_NonUnlockingOverlay_WhenUnlockingOverlayStillActive_KeepsCursorUnlocked()
    {
        int unlockingId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        int otherId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = false });
        router.ShowOverlay(unlockingId);
        router.ShowOverlay(otherId);

        router.HideOverlay(otherId);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void HideOverlay_OneOfTwoUnlockingOverlays_KeepsCursorUnlocked()
    {
        int id1 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        int id2 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        router.ShowOverlay(id1);
        router.ShowOverlay(id2);

        router.HideOverlay(id1);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void HideOverlay_WithInputMaps_ReenablesInputMaps_WhenNoOtherActiveOverlayDisablesThem()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");

        int id = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { inputMap } });
        router.ShowOverlay(id);

        router.HideOverlay(id);

        Assert.IsTrue(inputMap.enabled);
    }

    [Test]
    public void HideOverlay_SharedInputMap_StaysDisabled_WhenOtherActiveOverlayStillDisablesIt()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        int id1 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        int id2 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.ShowOverlay(id1);
        router.ShowOverlay(id2);

        router.HideOverlay(id1);

        Assert.IsFalse(sharedMap.enabled);
    }

    #endregion
}
