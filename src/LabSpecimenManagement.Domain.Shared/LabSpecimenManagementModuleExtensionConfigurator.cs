using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Users;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace LabSpecimenManagement;

public static class LabSpecimenManagementModuleExtensionConfigurator
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
            ConfigureExistingProperties();
            ConfigureExtraProperties();
        });
    }

    private static void ConfigureExistingProperties()
    {
        /* You can change max lengths for properties of the
         * entities defined in the modules used by your application.
         */
    }

    private static void ConfigureExtraProperties()
    {
        ObjectExtensionManager.Instance.Modules()
            .ConfigureIdentity(identity =>
            {
                identity.ConfigureUser(user =>
                {
                    user.AddOrUpdateProperty<Guid?>(
                        "DepartmentId",
                        property =>
                        {
                            property.Configuration[IdentityModuleExtensionConsts.ConfigurationNames.AllowUserToEdit] = false;
                        }
                    );

                    user.AddOrUpdateProperty<string>(
                        "Position",
                        property =>
                        {
                            property.Attributes.Add(new StringLengthAttribute(100));
                            property.Configuration[IdentityModuleExtensionConsts.ConfigurationNames.AllowUserToEdit] = true;
                        }
                    );

                    user.AddOrUpdateProperty<UserRole>(
                        "UserRole",
                        property =>
                        {
                            property.DefaultValue = UserRole.Viewer;
                            property.Configuration[IdentityModuleExtensionConsts.ConfigurationNames.AllowUserToEdit] = false;
                        }
                    );
                });
            });
    }
}
