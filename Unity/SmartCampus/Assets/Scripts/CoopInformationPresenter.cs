using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoopInformationPresenter : MonoBehaviour
{
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private GameObject[] informationVariants;
    [SerializeField] private bool hideAllUntilAssigned = true;

    private void OnEnable()
    {
        ResolveCoordinator();

        if (coopSessionCoordinator != null)
        {
            coopSessionCoordinator.SlotsChanged += HandleSlotsChanged;
        }

        ApplyLocalInformation();
    }

    private void OnDisable()
    {
        if (coopSessionCoordinator != null)
        {
            coopSessionCoordinator.SlotsChanged -= HandleSlotsChanged;
        }
    }

    public void ApplyLocalInformation()
    {
        if (informationVariants == null || informationVariants.Length == 0)
        {
            return;
        }

        ResolveCoordinator();

        var informationChannel = coopSessionCoordinator == null
            ? -1
            : coopSessionCoordinator.GetLocalInformationChannel(informationVariants.Length);

        for (var index = 0; index < informationVariants.Length; index++)
        {
            var variant = informationVariants[index];
            if (variant == null)
            {
                continue;
            }

            var shouldShow = informationChannel >= 0
                ? index == informationChannel
                : !hideAllUntilAssigned;

            variant.SetActive(shouldShow);
        }
    }

    private void HandleSlotsChanged()
    {
        ApplyLocalInformation();
    }

    private void ResolveCoordinator()
    {
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
    }
}
