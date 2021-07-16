namespace CUE4Parse.UE4.Objects.UObject
{
    /** Secondary condition to check before considering the replication of a lifetime property. */
    public enum ELifetimeCondition
    {
        COND_None = 0,							// This property has no condition, and will send anytime it changes
        COND_InitialOnly = 1,					// This property will only attempt to send on the initial bunch
        COND_OwnerOnly = 2,						// This property will only send to the actor's owner
        COND_SkipOwner = 3,						// This property send to every connection EXCEPT the owner
        COND_SimulatedOnly = 4,					// This property will only send to simulated actors
        COND_AutonomousOnly = 5,				// This property will only send to autonomous actors
        COND_SimulatedOrPhysics = 6,			// This property will send to simulated OR bRepPhysics actors
        COND_InitialOrOwner = 7,				// This property will send on the initial packet, or to the actors owner
        COND_Custom = 8,						// This property has no particular condition, but wants the ability to toggle on/off via SetCustomIsActiveOverride
        COND_ReplayOrOwner = 9,					// This property will only send to the replay connection, or to the actors owner
        COND_ReplayOnly = 10,					// This property will only send to the replay connection
        COND_SimulatedOnlyNoReplay = 11,		// This property will send to actors only, but not to replay connections
        COND_SimulatedOrPhysicsNoReplay = 12,	// This property will send to simulated Or bRepPhysics actors, but not to replay connections
        COND_SkipReplay = 13,					// This property will not send to the replay connection
        COND_Max = 14
        //COND_Never = 15,						// This property will never be replicated
        //COND_Max = 16
    }

    public enum ELifetimeRepNotifyCondition
    {
	    REPNOTIFY_OnChanged = 0,		// Only call the property's RepNotify function if it changes from the local value
	    REPNOTIFY_Always = 1,		// Always Call the property's RepNotify function when it is received from the server
    }

    public enum EChannelCloseReason : byte
    {
        Destroyed,
        Dormancy,
        LevelUnloaded,
        Relevancy,
        TearOff,
        /* reserved */
        MAX	= 15		// this value is used for serialization, modifying it may require a network version change
    }
}