using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Assemblies.UISystem;
using System.Linq;
using System;

public class ViewRouterTests
{
    private ViewRouter router;

    private class MockOverlayView : IOverlayView
    {
        public bool IsShown { get; private set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }
        public OverlayViewActions AssignedViewActions { get; private set; }

        public void OnShow(OverlayViewActions viewActions)
        {
            IsShown = true;
            ShowCallCount++;
            AssignedViewActions = viewActions;
        }

        public void OnHide() { IsShown = false; HideCallCount++; }
    }

    private class MockScreenView : IScreenView
    {
        public bool IsShown { get; private set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }
        public ScreenViewActions AssignedViewActions { get; private set; }

        public void OnShow(ScreenViewActions viewActions)
        {
            IsShown = true;
            ShowCallCount++;
            AssignedViewActions = viewActions;
        }

        public void OnHide() { IsShown = false; HideCallCount++; }
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

    #region ToggleOverlay

    [Test]
    public void ToggleOverlay_ShowsOverlayIfOverlayHidden()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });

        router.ToggleOverlay(id);
        Assert.AreEqual(1, view.ShowCallCount);
    }

    [Test]
    public void ToggleOverlay_HidesOverlayIfOverlayShown()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });

        router.ToggleOverlay(id);
        router.ToggleOverlay(id);
        Assert.AreEqual(1, view.HideCallCount);
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
    public void ShowOverlay_RegisteredOverlay_InjectsOverlayViewActions()
    {
        var view = new MockOverlayView();
        int id = router.RegisterOverlay(new OverlayOptions { view = view });

        router.ShowOverlay(id);

        Assert.AreEqual(id, view.AssignedViewActions.Id);

        view.AssignedViewActions.Dismiss();
        Assert.AreEqual(1, view.HideCallCount);
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

    #region RegisterScreenView

    [Test]
    public void RegisterScreenView_NullView_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => router.RegisterScreenView(new ScreenViewOptions { view = null }));
    }

    [Test]
    public void RegisterScreenView_Once_AddsToScreenViewsDictionary()
    {
        var view = new MockScreenView();
        var options = new ScreenViewOptions { view = view };

        int id = router.RegisterScreenView(options);

        Assert.IsTrue(router.ScreenViews.ContainsKey(id));
        Assert.AreEqual(view, router.ScreenViews[id].view);
    }

    [Test]
    public void RegisterScreenView_MultipleTimes_AssignsUniqueIds()
    {
        int id1 = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView() });
        int id2 = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView() });
        int id3 = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView() });

        Assert.AreNotEqual(id1, id2);
        Assert.AreNotEqual(id2, id3);
        Assert.AreNotEqual(id1, id3);
    }

    [Test]
    public void RegisterScreenView_InterleavedWithRegisterOverlay_AllIdsGloballyUnique()
    {
        int overlayId1 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });
        int screenId1 = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView() });
        int overlayId2 = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView() });
        int screenId2 = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView() });

        var ids = new[] { overlayId1, screenId1, overlayId2, screenId2 };
        Assert.AreEqual(ids.Length, ids.Distinct().Count());
    }

    #endregion

    #region PushScreenView

    [Test]
    public void PushScreenView_UnregisteredId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => router.PushScreenView(999));
    }

    [Test]
    public void PushScreenView_RegisteredView_SetsActiveScreenViewId()
    {
        var view = new MockScreenView();
        int id = router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(id);

        Assert.AreEqual(id, router.ActiveScreenViewId);
        Assert.AreEqual(1, router.ScreenViewHistory.Count);
        Assert.AreEqual(id, router.ScreenViewHistory[0]);
    }

    [Test]
    public void PushScreenView_RegisteredView_CallsViewShow()
    {
        var view = new MockScreenView();
        int id = router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(id);

        Assert.IsTrue(view.IsShown);
        Assert.AreEqual(1, view.ShowCallCount);
        Assert.AreEqual(id, view.AssignedViewActions.Id);
    }

    [Test]
    public void PushScreenView_SameIdAlreadyOnTop_IsNoOp()
    {
        var view = new MockScreenView();
        int id = router.RegisterScreenView(new ScreenViewOptions { view = view });
        router.PushScreenView(id);

        router.PushScreenView(id);

        Assert.AreEqual(1, view.ShowCallCount);
        Assert.AreEqual(0, view.HideCallCount);
        Assert.AreEqual(1, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PushScreenView_DifferentIdWhileAnotherActive_HidesPreviousAndShowsNew()
    {
        var viewA = new MockScreenView();
        var viewB = new MockScreenView();
        int idA = router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(idA);

        router.PushScreenView(idB);

        Assert.AreEqual(1, viewA.HideCallCount);
        Assert.AreEqual(1, viewB.ShowCallCount);
        Assert.AreEqual(idB, router.ActiveScreenViewId);
        Assert.AreEqual(2, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PushScreenView_FirstPush_InjectsNullBack()
    {
        var view = new MockScreenView();
        int id = router.RegisterScreenView(new ScreenViewOptions { view = view });

        router.PushScreenView(id);

        Assert.IsNull(view.AssignedViewActions.Back);
    }

    [Test]
    public void PushScreenView_SubsequentPush_InjectsNonNullBack()
    {
        var viewA = new MockScreenView();
        var viewB = new MockScreenView();
        int idA = router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(idA);

        router.PushScreenView(idB);

        Assert.IsNotNull(viewB.AssignedViewActions.Back);
    }

    [Test]
    public void PushScreenView_InjectedBack_PopsToPreviousScreen()
    {
        var viewA = new MockScreenView();
        var viewB = new MockScreenView();
        int idA = router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(idA);
        router.PushScreenView(idB);

        viewB.AssignedViewActions.Back();

        Assert.AreEqual(idA, router.ActiveScreenViewId);
        Assert.AreEqual(1, viewB.HideCallCount);
        Assert.AreEqual(2, viewA.ShowCallCount);
    }

    [Test]
    public void PushScreenView_WithUnlockCursor_UnlocksCursor()
    {
        int id = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = true });

        router.PushScreenView(id);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void PushScreenView_TopLocksCursor_ButUnderlyingScreenUnlocks_CursorStaysLocked()
    {
        int underId = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = true });
        int topId = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = false });
        router.PushScreenView(underId);

        router.PushScreenView(topId);

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsFalse(Cursor.visible);
    }

    [Test]
    public void PushScreenView_WithInputMaps_DisablesInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        inputMap.Enable();
        int id = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { inputMap } });

        router.PushScreenView(id);

        Assert.IsFalse(inputMap.enabled);
    }

    [Test]
    public void PushScreenView_SecondScreen_ReenablesFirstScreenUniqueInputMaps()
    {
        var mapA = new InputActionMap("mapA");
        mapA.AddAction("action");
        var mapB = new InputActionMap("mapB");
        mapB.AddAction("action");

        int idA = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { mapA } });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { mapB } });
        router.PushScreenView(idA);

        router.PushScreenView(idB);

        Assert.IsTrue(mapA.enabled);
        Assert.IsFalse(mapB.enabled);
    }

    [Test]
    public void PushScreenView_SharedInputMapBetweenOldAndNewTop_StaysDisabled()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        int idA = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.PushScreenView(idA);

        router.PushScreenView(idB);

        Assert.IsFalse(sharedMap.enabled);
    }

    #endregion

    #region PopScreenView

    [Test]
    public void PopScreenView_EmptyHistory_IsNoOp()
    {
        Assert.DoesNotThrow(() => router.PopScreenView());
        Assert.IsNull(router.ActiveScreenViewId);
    }

    [Test]
    public void PopScreenView_SingleItemHistory_HidesTopAndClearsActive()
    {
        var view = new MockScreenView();
        int id = router.RegisterScreenView(new ScreenViewOptions { view = view, unlockCursorWhenShown = true });
        router.PushScreenView(id);

        router.PopScreenView();

        Assert.AreEqual(1, view.HideCallCount);
        Assert.IsNull(router.ActiveScreenViewId);
        Assert.AreEqual(0, router.ScreenViewHistory.Count);
        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
    }

    [Test]
    public void PopScreenView_SingleItemHistoryWithInputMaps_ReenablesInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        int id = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { inputMap } });
        router.PushScreenView(id);

        router.PopScreenView();

        Assert.IsTrue(inputMap.enabled);
    }

    [Test]
    public void PopScreenView_MultiItemHistory_HidesTopAndReshowsPrevious()
    {
        var viewA = new MockScreenView();
        var viewB = new MockScreenView();
        int idA = router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(idA);
        router.PushScreenView(idB);

        router.PopScreenView();

        Assert.AreEqual(1, viewB.HideCallCount);
        Assert.AreEqual(2, viewA.ShowCallCount);
        Assert.AreEqual(idA, router.ActiveScreenViewId);
        Assert.AreEqual(1, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PopScreenView_PopsBackToRoot_ReshowsWithNullBack()
    {
        var viewA = new MockScreenView();
        var viewB = new MockScreenView();
        int idA = router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(idA);
        router.PushScreenView(idB);

        router.PopScreenView();

        Assert.IsNull(viewA.AssignedViewActions.Back);
    }

    #endregion

    #region PopAllScreenViews

    [Test]
    public void PopAllScreenViews_EmptyHistory_IsNoOp()
    {
        Assert.DoesNotThrow(() => router.PopAllScreenViews());
        Assert.IsNull(router.ActiveScreenViewId);
    }

    [Test]
    public void PopAllScreenViews_NonEmpty_HidesOnlyTopAndClears()
    {
        var viewA = new MockScreenView();
        var viewB = new MockScreenView();
        int idA = router.RegisterScreenView(new ScreenViewOptions { view = viewA });
        int idB = router.RegisterScreenView(new ScreenViewOptions { view = viewB });
        router.PushScreenView(idA);
        router.PushScreenView(idB);

        router.PopAllScreenViews();

        Assert.AreEqual(1, viewB.HideCallCount);
        Assert.AreEqual(1, viewA.HideCallCount); // hidden during the push of B, not during PopAll
        Assert.IsNull(router.ActiveScreenViewId);
        Assert.AreEqual(0, router.ScreenViewHistory.Count);
    }

    [Test]
    public void PopAllScreenViews_RefreshesCursorAndInputMaps()
    {
        var inputMap = new InputActionMap("testMap");
        inputMap.AddAction("action");
        int id = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = true, inputMapsToDisableWhenShown = new[] { inputMap } });
        router.PushScreenView(id);

        router.PopAllScreenViews();

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
        Assert.IsTrue(inputMap.enabled);
    }

    #endregion

    #region ScreenAndOverlayInteraction

    [Test]
    public void TopScreenUnlocksCursor_ActiveOverlayLocks_CursorUnlocked()
    {
        int screenId = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = true });
        int overlayId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = false });
        router.PushScreenView(screenId);

        router.ShowOverlay(overlayId);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void ActiveOverlayUnlocksCursor_TopScreenLocks_CursorUnlocked()
    {
        int screenId = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = false });
        int overlayId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        router.ShowOverlay(overlayId);

        router.PushScreenView(screenId);

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    [Test]
    public void SharedInputMap_BetweenScreenAndOverlay_StaysDisabled_WhileEitherActive()
    {
        var sharedMap = new InputActionMap("shared");
        sharedMap.AddAction("action");
        sharedMap.Enable();

        int screenId = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        int overlayId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), inputMapsToDisableWhenShown = new[] { sharedMap } });
        router.PushScreenView(screenId);
        router.ShowOverlay(overlayId);

        router.PopScreenView();

        Assert.IsFalse(sharedMap.enabled);

        router.HideOverlay(overlayId);

        Assert.IsTrue(sharedMap.enabled);
    }

    [Test]
    public void PopLastScreenView_WhileUnlockingOverlayActive_CursorStaysUnlocked()
    {
        int overlayId = router.RegisterOverlay(new OverlayOptions { view = new MockOverlayView(), unlockCursorWhenShown = true });
        int screenId = router.RegisterScreenView(new ScreenViewOptions { view = new MockScreenView(), unlockCursorWhenShown = false });
        router.ShowOverlay(overlayId);
        router.PushScreenView(screenId);

        router.PopScreenView();

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
        Assert.IsTrue(Cursor.visible);
    }

    #endregion
}
