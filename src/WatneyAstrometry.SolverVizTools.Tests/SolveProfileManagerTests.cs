using FluentAssertions;
using WatneyAstrometry.SolverVizTools.Exceptions;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;

namespace WatneyAstrometry.SolverVizTools.Tests
{
    public class SolveProfileManagerTests
    {
        [Fact]
        public void Should_generate_default_profiles()
        {
            var manager = new SolveSettingsManager(new SolveSettingsManager.Options()
            {
                ProfilesFileName = "testprofiles.json",
                StorageFolder = Path.GetTempPath()
            });

            var profiles = manager.GetProfiles(true, false);
            profiles.Count.Should().Be(2);
            
        }

        [Fact]
        public void Should_save_default_profiles_and_others_and_also_delete()
        {
            var opts = new SolveSettingsManager.Options()
            {
                ProfilesFileName = $"testprofiles-{Guid.NewGuid().ToString()}.json",
                StorageFolder = Path.GetTempPath()
            };

            var manager = new SolveSettingsManager(opts);

            var profiles = manager.GetProfiles(true, false);

            var profile = manager.CreateNewProfile("testprofile", SolveProfileType.Blind);
            profile.GenericOptions.MaxStars = 100;
            profile.BlindOptions.SearchOrder = SearchOrder.SouthFirst;

            manager.SaveProfiles();

            var newManager = new SolveSettingsManager(opts);
            profiles = newManager.GetProfiles(true, false);
            profiles.Count.Should().Be(3);

            var deletable = profiles.FirstOrDefault(x => x.IsDeletable);
            var notDeletable = profiles.FirstOrDefault(x => x.IsDeletable == false);

            Assert.Throws<SolveProfileException>(() => newManager.DeleteProfile(notDeletable));
            newManager.DeleteProfile(deletable);

            newManager = new SolveSettingsManager(opts);
            profiles = newManager.GetProfiles(true, false);
            profiles.Count.Should().Be(2);

        }
    }
}