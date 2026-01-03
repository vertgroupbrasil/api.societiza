namespace Subscription.Persistence.Seeding;

public static class SubscriptionSeedIds
{
    public static readonly Guid PlanTrial = Guid.Parse("a1b2c3d4-e5f6-4789-9012-34567890abcd");
    public static readonly Guid PlanEssential = Guid.Parse("b2c3d4e5-f678-4901-2345-67890abcdeff");
    public static readonly Guid PlanProfessional = Guid.Parse("c3d4e5f6-7890-4123-4567-890abcdef001");
    public static readonly Guid PlanEnterprise = Guid.Parse("d4e5f678-9012-4345-6789-0abcdef00112");

    public static readonly Guid FeatureTrialMaxUsers = Guid.Parse("e5f67890-1234-4567-8901-23abcdef0123");
    public static readonly Guid FeatureTrialMaxWorkflows = Guid.Parse("f6789012-3456-5678-9012-34bcdef01234");
    public static readonly Guid FeatureTrialMaxStorage = Guid.Parse("67890123-4567-6789-0123-45cdef012345");
    public static readonly Guid FeatureTrialWorkflowEnabled = Guid.Parse("78901234-5678-7890-1234-56def0123456");

    public static readonly Guid FeatureEssentialMaxUsers = Guid.Parse("89012345-6789-8901-2345-67ef01234567");
    public static readonly Guid FeatureEssentialMaxWorkflows = Guid.Parse("90123456-7890-9012-3456-78f012345678");
    public static readonly Guid FeatureEssentialMaxStorage = Guid.Parse("01234567-8901-0123-4567-89a123456789");
    public static readonly Guid FeatureEssentialWorkflowEnabled = Guid.Parse("12345678-9012-1234-5678-9ab234567890");

    public static readonly Guid FeatureProfessionalMaxUsers = Guid.Parse("23456789-0123-2345-6789-abcd34567890");
    public static readonly Guid FeatureProfessionalMaxWorkflows = Guid.Parse("34567890-1234-3456-7890-bcde45678901");
    public static readonly Guid FeatureProfessionalMaxStorage = Guid.Parse("45678901-2345-4567-8901-cdef56789012");
    public static readonly Guid FeatureProfessionalWorkflowEnabled = Guid.Parse("56789012-3456-5678-9012-def678901234");

    public static readonly Guid FeatureEnterpriseMaxUsers = Guid.Parse("67890123-4567-6789-0123-ef7890123456");
    public static readonly Guid FeatureEnterpriseMaxWorkflows = Guid.Parse("78901234-5678-7890-1234-f89012345678");
    public static readonly Guid FeatureEnterpriseMaxStorage = Guid.Parse("89012345-6789-8901-2345-901234567890");
    public static readonly Guid FeatureEnterpriseWorkflowEnabled = Guid.Parse("90123456-7890-9012-3456-012345678901");
}