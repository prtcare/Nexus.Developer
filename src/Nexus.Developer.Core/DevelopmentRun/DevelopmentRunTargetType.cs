namespace Nexus.Developer.Core.DevelopmentRun;

// M-07-10.3 outcome: "RUN-##### can be allocated against a Task/Feature/Issue."
// The DEVELOP control may be visible on more object types in the UI
// (WI-07-10.3.2 lists Project/Subproject/Feature/Milestone/Task/Subtask/Issue),
// but an actual DevelopmentRun row only ever targets one of these three.
public enum DevelopmentRunTargetType
{
    Feature = 1,
    Task = 2,
    Issue = 3
}
