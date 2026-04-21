using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Assemblies.UISystem;
using System;

public class ViewRouterTests
{
    private ViewRouter router;

    private abstract class MockOverlayViewBase : IOverlayView
    {
        public bool IsShown { get; private set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }
        public OverlayViewActions AssignedViewActions { get; private set; }

        public abstract string Key { get; }

        public void OnShow(OverlayViewActions viewActions)
        {
            IsShown = true;
            ShowCallCount++;
            AssignedViewActions = viewActions;
        }

        public void OnHide() { IsShown = false; HideCallCount++; }
    }

    private class MockOverlayViewA : MockOverlayViewBase
    {
        public override string Key => nameof(MockOverlayViewA);
    }

    private class MockOverlayViewB : MockOverlayViewBase
    {
        public override string Key => nameof(MockOverlayViewB);
    }

    private abstract class MockScreenViewBase : IScreenView
    {
        public bool IsShown { get; private set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }
        public ScreenViewActions AssignedViewActions { get; private set; }

        public abstract string Key { get; }

        public void OnShow(ScreenViewActions viewActions)
        {
            IsShown = true;
            ShowCallCount++;
            AssignedViewActions = viewActions;
        }

        public void OnHide() { IsShown = false; HideCallCount++; }
    }

    private class MockScreenViewA : MockScreenViewBase
    {
        public override string Key => nameof(MockScreenViewA);
    }

    private class MockScreenViewB : MockScreenViewBase
    {
        public override string Key => nameof(MockScreenViewB);
    }

    [SetUp]
    public void SetUp()
    {
        router = new ViewRouter();
    }

    #region RegisterOverlay

    [Test]
    public void RegisterOverlay_NullView_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => router.RegisterOverlay(new OverlayOptions { view = null }));
    }

    [Test]
    public void RegisterOverlay_Once_AddsToOverlayDictionary()
    {
        var view = new MockOverlayViewA();
        var options = new OverlayOptions { view = view };

        router.RegisterOverlay(options);

        Assert.IsTrue(router.Overlays.ContainsKey(view.Key));
        Assert.AreEqual(view, router.Overlays[view.Key].view);
    }

    [Test]
    public void RegisterOverlay_DuplicateKey_ThrowsArgumentException()
    {
        router.RegisterOverlay(new OverlayOptions { view = new MockOverlayViewA() });

        Assert.Throws<ArgumentException>(() =>
            router.RegisterOverlay(new OverlayOptions { view = new MockOverlayViewA() }));
    }

    #endregion

    #region ToggleOverlay

    [Test]
    public void ToggleOverlay_ShowsOverlayIfOverlayHidden()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });

        router.ToggleOverlay(view.Key);
        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void ToggleOverlay_HidesOverlayIfOverlayShown()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });

        router.ToggleOverlay(view.Key);
        router.ToggleOverlay(view.Key);
        Assert.AreEqual(1, view.HideCallCount);
    }

    [Test]
    public void ToggleOverlay_UnregisteredKey_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => router.ToggleOverlay("nonexistent"));
    }

    #endregion

    #region ShowOverlay

    [Test]
    public void ShowOverlay_UnregisteredKey_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => router.ShowOverlay("nonexistent"));
    }


    [Test]
    public void ShowOverlay_AlreadyShownOverlay_IsNoOp()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });
        router.ShowOverlay(view.Key);

        router.ShowOverlay(view.Key);

        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void ShowOverlay_RegisteredOverlay_AddsToActiveOverlayKeys()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });

        router.ShowOverlay(view.Key);

        Assert.IsTrue(router.ActiveOverlayKeys.Contains(view.Key));
    }

    [Test]
    public void ShowOverlay_RegisteredOverlay_CallsViewShow()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });

        router.ShowOverlay(view.Key);

        Assert.IsTrue(view.IsShown);
        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void ShowOverlay_RegisteredOverlay_InjectsDismissAction()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });

        router.ShowOverlay(view.Key);

        view.AssignedViewActions.Dismiss();
        Assert.AreEqual(1, view.HideCallCount);
    }

    [Test]
    public void ShowOverlay_WithUnlockCursor_UnlocksCursor()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view, unlockCursorWhenShown = true });

        router.ShowOverlay(view.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void ShowOverlay_WithoutUnlockCursor_LocksCursor()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view, unlockCursorWhenShown = false });

        router.ShowOverlay(view.Key);

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsFalse(Cursor.visible);
    }

    [Test]
    public void ShowOverlay_WithoutUnlockCursor_WhenOtherActiveOverlayUnlocks_KeepsCursorUnlocked()
    {
        var unlocking = new MockOverlayViewA();
        var locking = new MockOverlayViewB();
        router.RegisterOverlay(new OverlayOptions { view = unlocking, unlockCursorWhenShown = true });
        router.RegisterOverlay(new OverlayOptions { view = locking, unlockCursorWhenShown = false });
        router.ShowOverlay(unlocking.Key);

        router.ShowOverlay(locking.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void ShowOverlay_WithInputMaps_DisablesInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        inputMap.Enable();
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view, inputMapsToDisableWhenShown = new[] { inputMap } });

        router.ShowOverlay(view.Key);

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

        var active = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = active, inputMapsToDisableWhenShown = new[] { activeMap } });
        router.RegisterOverlay(new OverlayOptions { view = new MockOverlayViewB(), inputMapsToDisableWhenShown = new[] { inactiveMap } });

        router.ShowOverlay(active.Key);

        Assert.IsTrue(inactiveMap.enabled);
    }

    [Test]
    public void ShowOverlay_SharedInputMap_NotReenabledWhenAlsoInActiveOverlay()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        var active = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = active, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.RegisterOverlay(new OverlayOptions { view = new MockOverlayViewB(), inputMapsToDisableWhenShown = new[] { sharedMap } });

        router.ShowOverlay(active.Key);

        Assert.IsFalse(sharedMap.enabled);
    }

    #endregion

    #region HideOverlay

    [Test]
    public void HideOverlay_NonActiveOverlay_IsNoOp()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });

        router.HideOverlay(view.Key);

        Assert.AreEqual(0, view.HideCallCount);
        Assert.IsFalse(router.ActiveOverlayKeys.Contains(view.Key));
    }

    [Test]
    public void HideOverlay_ActiveOverlay_RemovesFromActiveOverlayKeys()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });
        router.ShowOverlay(view.Key);

        router.HideOverlay(view.Key);

        Assert.IsFalse(router.ActiveOverlayKeys.Contains(view.Key));
    }

    [Test]
    public void HideOverlay_ActiveOverlay_CallsViewHide()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view });
        router.ShowOverlay(view.Key);

        router.HideOverlay(view.Key);

        Assert.IsFalse(view.IsShown);
        Assert.AreEqual(1, view.HideCallCount);
    }

    [Test]
    public void HideOverlay_LastUnlockingOverlay_LocksCursor()
    {
        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view, unlockCursorWhenShown = true });
        router.ShowOverlay(view.Key);

        router.HideOverlay(view.Key);

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsFalse(Cursor.visible);
    }

    [Test]
    public void HideOverlay_NonUnlockingOverlay_WhenUnlockingOverlayStillActive_KeepsCursorUnlocked()
    {
        var unlocking = new MockOverlayViewA();
        var other = new MockOverlayViewB();
        router.RegisterOverlay(new OverlayOptions { view = unlocking, unlockCursorWhenShown = true });
        router.RegisterOverlay(new OverlayOptions { view = other, unlockCursorWhenShown = false });
        router.ShowOverlay(unlocking.Key);
        router.ShowOverlay(other.Key);

        router.HideOverlay(other.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void HideOverlay_OneOfTwoUnlockingOverlays_KeepsCursorUnlocked()
    {
        var a = new MockOverlayViewA();
        var b = new MockOverlayViewB();
        router.RegisterOverlay(new OverlayOptions { view = a, unlockCursorWhenShown = true });
        router.RegisterOverlay(new OverlayOptions { view = b, unlockCursorWhenShown = true });
        router.ShowOverlay(a.Key);
        router.ShowOverlay(b.Key);

        router.HideOverlay(a.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void HideOverlay_WithInputMaps_ReenablesInputMaps_WhenNoOtherActiveOverlayDisablesThem()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");

        var view = new MockOverlayViewA();
        router.RegisterOverlay(new OverlayOptions { view = view, inputMapsToDisableWhenShown = new[] { inputMap } });
        router.ShowOverlay(view.Key);

        router.HideOverlay(view.Key);

        Assert.IsTrue(inputMap.enabled);
    }

    [Test]
    public void HideOverlay_SharedInputMap_StaysDisabled_WhenOtherActiveOverlayStillDisablesIt()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        var a = new MockOverlayViewA();
        var b = new MockOverlayViewB();
        router.RegisterOverlay(new OverlayOptions { view = a, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.RegisterOverlay(new OverlayOptions { view = b, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.ShowOverlay(a.Key);
        router.ShowOverlay(b.Key);

        router.HideOverlay(a.Key);

        Assert.IsFalse(sharedMap.enabled);
    }

    #endregion

    #region RegisterScreenView

    [Test]
    public void RegisterScreenView_NullView_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => router.RegisterScreenView(new ScreenViewOptions { view = null }));
    }

    [Test]
    public void RegisterScreenView_Once_AddsToScreenViewsDictionary()
    {
        var view = new MockScreenViewA();
        var options = new ScreenViewOptions { view = view };

        router.RegisterScreenView(options);

        Assert.IsTrue(router.ScreenViews.ContainsKey(view.Key));
        Assert.AreEqual(view, router.ScreenViews[view.Key].view);
    }

    [Test]
    public void RegisterScreenView_DuplicateKey_ThrowsArgumentException()
    {
        router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenViewA() });

        Assert.Throws<ArgumentException>(() =>
            router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenViewA() }));
    }

    #endregion

    #region PushScreenView

    [Test]
    public void PushScreenView_UnregisteredKey_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => router.PushScreenView("nonexistent"));
    }

    [Test]
    public void PushScreenView_RegisteredView_SetsActiveScreenViewKey()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(view.Key);

        Assert.AreEqual(view.Key, router.ActiveScreenViewKey);
        Assert.AreEqual(1, router.ScreenViewHistory.Count);
        Assert.AreEqual(view.Key, router.ScreenViewHistory[0]);
    }

    [Test]
    public void PushScreenView_RegisteredView_CallsViewShow()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(view.Key);

        Assert.IsTrue(view.IsShown);
        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void PushScreenView_SameKeyAlreadyOnTop_IsNoOp()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view });
        router.PushScreenView(view.Key);

        router.PushScreenView(view.Key);

        Assert.AreEqual(1, view.ShowCallCount);
        Assert.AreEqual(0, view.HideCallCount);
        Assert.AreEqual(1, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PushScreenView_DifferentKeyWhileAnotherActive_HidesPreviousAndShowsNew()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);

        router.PushScreenView(viewB.Key);

        Assert.AreEqual(1, viewA.HideCallCount);
        Assert.AreEqual(1, viewB.ShowCallCount);
        Assert.AreEqual(viewB.Key, router.ActiveScreenViewKey);
        Assert.AreEqual(2, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PushScreenView_FirstPush_InjectsNullBack()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(view.Key);

        Assert.IsNull(view.AssignedViewActions.Back);
    }

    [Test]
    public void PushScreenView_SubsequentPush_InjectsNonNullBack()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);

        router.PushScreenView(viewB.Key);

        Assert.IsNotNull(viewB.AssignedViewActions.Back);
    }

    [Test]
    public void PushScreenView_InjectsShowScreenAction()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(view.Key);

        Assert.IsNotNull(view.AssignedViewActions.ShowScreen);
    }

    [Test]
    public void PushScreenView_InjectedShowScreen_PushesAnotherScreen()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);

        viewA.AssignedViewActions.ShowScreen(viewB.Key);

        Assert.AreEqual(viewB.Key, router.ActiveScreenViewKey);
        Assert.AreEqual(2, router.ScreenViewHistory.Count);
        Assert.AreEqual(1, viewA.HideCallCount);
        Assert.AreEqual(1, viewB.ShowCallCount);
    }

    [Test]
    public void PushScreenView_InjectsShowOverlayAction()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(view.Key);

        Assert.IsNotNull(view.AssignedViewActions.ShowOverlay);
    }

    [Test]
    public void PushScreenView_InjectedShowOverlay_ShowsOverlay()
    {
        var screen = new MockScreenViewA();
        var overlay = new MockOverlayViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = screen });
        router.RegisterOverlay(new OverlayOptions { view = overlay });
        router.PushScreenView(screen.Key);

        screen.AssignedViewActions.ShowOverlay(overlay.Key);

        Assert.AreEqual(1, overlay.ShowCallCount);
        Assert.IsTrue(router.ActiveOverlayKeys.Contains(overlay.Key));
    }

    [Test]
    public void PopScreenView_ReshowsPrevious_InjectsNonNullShowScreenAndShowOverlay()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);
        router.PushScreenView(viewB.Key);

        router.PopScreenView();

        Assert.IsNotNull(viewA.AssignedViewActions.ShowScreen);
        Assert.IsNotNull(viewA.AssignedViewActions.ShowOverlay);
    }

    [Test]
    public void PushScreenView_InjectedBack_PopsToPreviousScreen()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);
        router.PushScreenView(viewB.Key);

        viewB.AssignedViewActions.Back();

        Assert.AreEqual(viewA.Key, router.ActiveScreenViewKey);
        Assert.AreEqual(1, viewB.HideCallCount);
        Assert.AreEqual(2, viewA.ShowCallCount);
    }

    [Test]
    public void PushScreenView_WithUnlockCursor_UnlocksCursor()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view, unlockCursorWhenShown = true });

        router.PushScreenView(view.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void PushScreenView_TopLocksCursor_ButUnderlyingScreenUnlocks_CursorStaysLocked()
    {
        var under = new MockScreenViewA();
        var top = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = under, unlockCursorWhenShown = true });
        router.RegisterScreenView(new ScreenViewOptions { view = top, unlockCursorWhenShown = false });
        router.PushScreenView(under.Key);

        router.PushScreenView(top.Key);

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsFalse(Cursor.visible);
    }

    [Test]
    public void PushScreenView_WithInputMaps_DisablesInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        inputMap.Enable();
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view, inputMapsToDisableWhenShown = new[] { inputMap } });

        router.PushScreenView(view.Key);

        Assert.IsFalse(inputMap.enabled);
    }

    [Test]
    public void PushScreenView_SecondScreen_ReenablesFirstScreenUniqueInputMaps()
    {
        var mapA = new InputActionMap("mapA");
        mapA.AddAction("action");
        var mapB = new InputActionMap("mapB");
        mapB.AddAction("action");

        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA, inputMapsToDisableWhenShown = new[] { mapA } });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB, inputMapsToDisableWhenShown = new[] { mapB } });
        router.PushScreenView(viewA.Key);

        router.PushScreenView(viewB.Key);

        Assert.IsTrue(mapA.enabled);
        Assert.IsFalse(mapB.enabled);
    }

    [Test]
    public void PushScreenView_SharedInputMapBetweenOldAndNewTop_StaysDisabled()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.PushScreenView(viewA.Key);

        router.PushScreenView(viewB.Key);

        Assert.IsFalse(sharedMap.enabled);
    }

    #endregion

    #region PopScreenView

    [Test]
    public void PopScreenView_EmptyHistory_IsNoOp()
    {
        Assert.DoesNotThrow(() => router.PopScreenView());
        Assert.IsNull(router.ActiveScreenViewKey);
    }

    [Test]
    public void PopScreenView_SingleItemHistory_HidesTopAndClearsActive()
    {
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view, unlockCursorWhenShown = true });
        router.PushScreenView(view.Key);

        router.PopScreenView();

        Assert.AreEqual(1, view.HideCallCount);
        Assert.IsNull(router.ActiveScreenViewKey);
        Assert.AreEqual(0, router.ScreenViewHistory.Count);
        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
    }

    [Test]
    public void PopScreenView_SingleItemHistoryWithInputMaps_ReenablesInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view, inputMapsToDisableWhenShown = new[] { inputMap } });
        router.PushScreenView(view.Key);

        router.PopScreenView();

        Assert.IsTrue(inputMap.enabled);
    }

    [Test]
    public void PopScreenView_MultiItemHistory_HidesTopAndReshowsPrevious()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);
        router.PushScreenView(viewB.Key);

        router.PopScreenView();

        Assert.AreEqual(1, viewB.HideCallCount);
        Assert.AreEqual(2, viewA.ShowCallCount);
        Assert.AreEqual(viewA.Key, router.ActiveScreenViewKey);
        Assert.AreEqual(1, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PopScreenView_PopsBackToRoot_ReshowsWithNullBack()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);
        router.PushScreenView(viewB.Key);

        router.PopScreenView();

        Assert.IsNull(viewA.AssignedViewActions.Back);
    }

    #endregion

    #region PopAllScreenViews

    [Test]
    public void PopAllScreenViews_EmptyHistory_IsNoOp()
    {
        Assert.DoesNotThrow(() => router.PopAllScreenViews());
        Assert.IsNull(router.ActiveScreenViewKey);
    }

    [Test]
    public void PopAllScreenViews_NonEmpty_HidesOnlyTopAndClears()
    {
        var viewA = new MockScreenViewA();
        var viewB = new MockScreenViewB();
        router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(viewA.Key);
        router.PushScreenView(viewB.Key);

        router.PopAllScreenViews();

        Assert.AreEqual(1, viewB.HideCallCount);
        Assert.AreEqual(1, viewA.HideCallCount); // hidden during the push of B, not during PopAll
        Assert.IsNull(router.ActiveScreenViewKey);
        Assert.AreEqual(0, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PopAllScreenViews_RefreshesCursorAndInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        var view = new MockScreenViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = view, unlockCursorWhenShown = true, inputMapsToDisableWhenShown = new[] { inputMap } });
        router.PushScreenView(view.Key);

        router.PopAllScreenViews();

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsTrue(inputMap.enabled);
    }

    #endregion

    #region ScreenAndOverlayInteraction

    [Test]
    public void TopScreenUnlocksCursor_ActiveOverlayLocks_CursorUnlocked()
    {
        var screen = new MockScreenViewA();
        var overlay = new MockOverlayViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = screen, unlockCursorWhenShown = true });
        router.RegisterOverlay(new OverlayOptions { view = overlay, unlockCursorWhenShown = false });
        router.PushScreenView(screen.Key);

        router.ShowOverlay(overlay.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void ActiveOverlayUnlocksCursor_TopScreenLocks_CursorUnlocked()
    {
        var screen = new MockScreenViewA();
        var overlay = new MockOverlayViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = screen, unlockCursorWhenShown = false });
        router.RegisterOverlay(new OverlayOptions { view = overlay, unlockCursorWhenShown = true });
        router.ShowOverlay(overlay.Key);

        router.PushScreenView(screen.Key);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void SharedInputMap_BetweenScreenAndOverlay_StaysDisabled_WhileEitherActive()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        var screen = new MockScreenViewA();
        var overlay = new MockOverlayViewA();
        router.RegisterScreenView(new ScreenViewOptions { view = screen, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.RegisterOverlay(new OverlayOptions { view = overlay, inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.PushScreenView(screen.Key);
        router.ShowOverlay(overlay.Key);

        router.PopScreenView();

        Assert.IsFalse(sharedMap.enabled);

        router.HideOverlay(overlay.Key);

        Assert.IsTrue(sharedMap.enabled);
    }

    [Test]
    public void PopLastScreenView_WhileUnlockingOverlayActive_CursorStaysUnlocked()
    {
        var overlay = new MockOverlayViewA();
        var screen = new MockScreenViewA();
        router.RegisterOverlay(new OverlayOptions { view = overlay, unlockCursorWhenShown = true });
        router.RegisterScreenView(new ScreenViewOptions { view = screen, unlockCursorWhenShown = false });
        router.ShowOverlay(overlay.Key);
        router.PushScreenView(screen.Key);

        router.PopScreenView();

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    #endregion
}
