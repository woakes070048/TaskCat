﻿namespace TaskCat.Data.Model
{
    using MongoDB.Bson.Serialization.Attributes;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using Utility;
    using Identity.Response;
    using Core;

    [BsonIgnoreExtraElements(Inherited = true)]
    public abstract class JobTask : ObservableObject
    {
        protected string Name;
        public string id { get; protected set; }

        [JsonIgnore]
        [BsonIgnore]
        protected JobTask Predecessor;

        //FIXME: The result type would definitelty not be string of course
        public delegate void JobTaskCompletedEventHandler(JobTask sender, JobTaskResult result);
        public event JobTaskCompletedEventHandler JobTaskCompleted;

        public delegate void AssetUpdatedEventHandler(string AssetRef, AssetModel asset);
        public event AssetUpdatedEventHandler AssetUpdated;

        //FIXME: I still dont know how Im going to implement this!
        //protected delegate void JobTaskUpdatedEventHandler(JobTask sender, string result);
        //protected event JobTaskUpdatedEventHandler JobTaskUpdated;

        [BsonIgnore]
        [JsonIgnore]
        public JobTaskResult Result { get; protected set; }

        public string Type { get; set; }

        [BsonIgnore]
        public string JobTaskStateString
        {
            get { return StateStringGenerator.GenerateStateString(State, Name); }
        }

        private JobTaskState state;
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobTaskState State
        {
            get
            {
                return state;
            }
            set
            {
                Set(ref state, value);           
            }
        }
        
        private AssetModel asset;
        [JsonIgnore]
        [BsonIgnore]
        public AssetModel Asset {
            get { return asset; }
            set {
                asset = value;
                if(value!=null)
                    this.AssetRef = asset.UserId;
            }
        }

        private string assetRef;
        public string AssetRef
        {
            get
            {
                return assetRef;
            }
            set
            {
                assetRef = value;
                if (AssetUpdated != null)
                {
                    if (Asset == null)
                        throw new InvalidOperationException("Invoking Asset Updated event without having actual Asset defined, error");
                    AssetUpdated(assetRef, Asset);
                }

            }
        }

        [BsonIgnoreIfNull]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ETA { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? InitiationTime { get; set; }
        public DateTime? ModifiedTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public TimeSpan? Duration
        {
            get
            {
                if (CompletionTime.HasValue && InitiationTime.HasValue)
                {
                    return CompletionTime.Value.Subtract(InitiationTime.Value);
                }
                else if (InitiationTime.HasValue)
                {
                    return DateTime.UtcNow.Subtract(InitiationTime.Value);
                }
                return null;
            }
        }

        [BsonIgnoreIfNull]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
        [BsonIgnoreIfNull]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Notes { get; set; }

        public bool IsReadytoMoveToNextTask { get; set; }
        public bool IsDependencySatisfied { get; set; }

        public bool IsStartingTask { get; set; } = true;
        public bool IsTerminatingTask { get; set; } = false;

        public bool ETAFailed
        {
            get
            {
                if (this.ETA.HasValue && this.State == JobTaskState.IN_PROGRESS)
                    return DateTime.UtcNow.Subtract(this.ETA.Value).TotalSeconds > 0;
                else if (this.ETA.HasValue && this.CompletionTime.HasValue)
                    return CompletionTime > ETA;
                return false;
            }
        }

        public JobTask()
        {

        }

        public JobTask(string type, string name) : this()
        {
            id = Guid.NewGuid().ToString();
            Type = type;
            Name = name;

            CreateTime = DateTime.Now;
            ModifiedTime = DateTime.Now;
        }

        public abstract void UpdateTask();
        public virtual JobTaskResult SetResultToNextState()
        {
            return this.Result;
        }

        public virtual void SetPredecessor(JobTask task, bool validateDependency = true)
        {
            //FIXME: This is weird, just plain weird
            if(validateDependency)
            {
                if (task.Result == null)
                    throw new ArgumentNullException(nameof(task.Result), "Predecessor Task Result is null, please initialize predecessor Task result in consrtuctor before setting it as a predecessor");
            }
            
            this.Predecessor = task;
            IsStartingTask = false;
           
        }

        public virtual void UpdateStateParams()
        {
            if (State == JobTaskState.PENDING) return;

            if (State >= JobTaskState.IN_PROGRESS)
                InitiationTime = InitiationTime ?? DateTime.UtcNow;

            if (IsReadytoMoveToNextTask)
                NotifyJobTaskCompleted();
        }

        protected virtual void NotifyJobTaskCompleted()
        {
            if (!IsReadytoMoveToNextTask)
                throw new InvalidOperationException("JobTask is not ready to move to next task, yet COMPLETED STATE ACHIEVED");
       
            this.CompletionTime = DateTime.UtcNow;
            this.InitiationTime = this.InitiationTime ?? this.CompletionTime;
            State = JobTaskState.COMPLETED;
            //FIXME: the JobTaskResult type has to be initiated
            if (JobTaskCompleted != null)
            {
                Result = SetResultToNextState();
                if (Result != null && Result.TaskCompletionTime == null)
                {
                    Result.TaskCompletionTime = CompletionTime;
                }
                JobTaskCompleted(this, Result);
            }
        }

    }

    //Should be moved to a new class file? No? Okay!

    public abstract class JobTaskResult
    {
        public DateTime? TaskCompletionTime { get; set; }       
        public Type ResultType { get; set; }
    }

    //FIXME: Might need to move this to an Util function
    public static class JobTaskResultExtensions
    {
        public static T Cast<T>(this object input)
        {
            return (T)input;
        }
    }
}
