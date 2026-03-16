
# Overview

This document contains the message architecture for sencilla framework

## Fluent Api Configuration

``` c#


// UseSencillaMessaging can be called multiple times
// if used in one app the config will be merged, if conflict on the value field use the one which is defined last
services.UseSencillaMessaging(o => 
{
    // some setup/common logic/configuration
    o.WithOptions(...);
    
    // Define custom middleware globally for all providers
    o.UseOpenTelemetry(); // add open telemetry
    o.UseUserIdInMessage(); // defined extension in user component, will set userId in the message from the userId middleware
    o.UseMiddleware<MyCustomMessageMiddleware>(); // some custom middleware without extension
    
    o.UseInMemoryQueue(c => 
    {
        // Load all below configuration from config
        c.LoadFromConfig(...);
        c.LoadConfigFromAssembly(...); // load queues, topics, handlers from config (attributes and interface configuration IMessagingConfig .. use same interfaces as in configuration) 
        
        // provider specific middleware 
        c.UseLogging(); // 
        c.UseMiddleware<MyInMemmoryCustomMiddleware>();
        
        // This is he common settings that will be applied to the middleware level 
        // and for all queues, override global one 
        c.WithOptions(s => { s.AutoAckn = true; /* other ... */ });
        
        // Routing for the messages 
        // Also can be loaded from attribute if Route is not mentioned at all here 
        c.Route(r => 
        {
            r.Message<MyMessage>().To("queue1");
            r.Messages(typeof(MyMessage), typeof(MyMessage2), ... ).To("queue1");
        })
        
        // Usually configured in other apps, but can be configured in the same app as well no restrictions
        // so we can listen one queue and publish to another 
        q.AddConsumerFor(["queue1", "queue2", "topic:subscription"], con => 
        {
            con.WithOptions(co => { co.MaxRetries; /*...*/ });
            
            // Add for all messages if no more handler 
            con.LoadHandlersFromAssembly(); // be default if not mentioned 
            con.Process<MyQueueProcessor>(); // receive row data from the queue and can do custom parse and handle logic
                                               // override default logic where it tries to parse messages by namespace/className 
            // or like below, this is default processor if not created above for consumer
            con.Process(c => 
            { 
                c.ByNamespace();
                c.ByType(); 
            });                                   
            
            // or add filtering for specific one   
            con.HandleOnly<MyMessage>(); // this also can be loaded from config if we use namespace similar to below 
            con.HandleOnly("Sencilla.Entity.MyUsefulModel", "...", ... ) 
            con.HandleOnly(typeof(MyMessage), ... );
            con.HandleOnly(nameof(MyMessage)); // Add processing by string so we can load this from config 
            
            con.MaxConsumerPerQueue = 5; // means every queue will have 5 consumers, default 1  
        })
        
        // or short version 
        q.AddConsumerFor("queue", c => c.LoadHandlersFromAssembly(...) ); // handle everything  
        q.AddConsumerFor<MyCommand>("queue"); // 
        q.AddConsumerFor("queue", [typeof(Command), typeof(Command2)]); // By default load handlers from assembly  
        q.AddConsumerFor("queue", ["Command1", "Command2"]); // By default load handlers assembly  
        
        // Below all methods are the same, they are aliases and can be used interchangibly
        // TODO: Think if we need it
        c.DefineQueue();
        c.DefineQueues(q => q.CreatePerMessageType()); // create new queue per message types
        c.UseQueue();
        c.ForQueue("queue", ...)
        c.ForQueues(["queue", "queue2"], q => 
        {
            q.WithOptions(...) // override global and middleware
            q.Support<MessageType>();
            q.Receive(typeof(MessageType1), typeof(MessageType2), ...)

            // override/merge with middleware configuration
            q.Route<...>(...)
            
            // Usually configured in other apps, but can be configured in the same app as well no restrictions
            // override common one
            q.AddConsumer(con => 
            {
                con.MaxConcumer = 3; // override middleware one
                con.HandleOnly<...>(...);
                con.Process<>();
                con.LoadHandlersFromAssembly();
                // con.UseMediator();
            })
        });
        
        // If provider supports it 
        c.DefineTopic(t => 
        {
            t.WithOptions(...);
            //...
            
            
        }); 
        
    });
    
    o.UseRabbitMQ( c => 
    {
        c.WithOptions() // The options is inherited from Base class and extend it with provider specific settings 
        
        // 
        c.DefineQueue() //
        c.ForQueue("queue") // if same queue is defined in few middlewares the messages will be send to both of them 
        
        // ...
    }) 
    
    o.UseMediator(c => 
    {
        c.AllowAll(); // Default config process all messages that comes to this pipeline
        c.Disable<TMessage>(); // It means that this message will not be processed and will be skipped
        c.Disable(typeof(TMessage1), typeof(TMessage2), /* ... */ )
        
        c.DisableAll(); // Disable all message 
        c.Allow<TMessage>() // 
    })
    
    // or just without config
    o.UseMediator();
});

```

## Attribute Configuration

Most of the FluentApi Configuration can be done using Attributes

``` c#

[Stream("queue_or_topic")]
class MyCommandA
{
    
}

// This will handle only messages from stream 
[Stream("queue_or_topic", MaxRetries = 3, Instance = 5)]
class MyCommandHandler<MyAnotherCommand>
{
    HandleAsync( MyAnotherCommand)
}

// Similar approach to what we have in EntityFramework, then this config can be load from assembly 
class MyStreamConfig: IMessagingConfig 
{
    Config(MessagingConfig options) {
        // ...
        // similar to what we have in the startup
    }
}

// the stream attribute can have configuration 

class Streams 
{
    // 
    static readonly QueueConfig Users = new { Name="users"; AutoLock = true };
    static readonly TopicConfig Companies = new { Name="companies"; AutoLock = true };
    
}

[Stream(Streams.Users)] 
class MyCommandB
{
    
}

```
