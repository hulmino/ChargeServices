using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backend
{
    public interface IMessageQueue
    {
        void Subscribe(Action<IIdEntity, string> subscriber);
        
        /// <summary>
        /// Notify subscribers. This is a nonblocking action and will be done asynchronously in the background.
        /// </summary>
        /// <param name="charger"></param>
        /// <param name="changedProperty"></param>
        void NotifyChange(IIdEntity charger, string changedProperty);
    }
    
    public class MessageQueue : IMessageQueue
    {
        private ConcurrentBag<Action<IIdEntity, string>> _subscribers = new();

        public void Subscribe(Action<IIdEntity, string> subscriber)
        {
            _subscribers.Add(subscriber);
        }

        public void NotifyChange(IIdEntity entity, string changedProperty)
        {
            var property = entity.GetType().GetProperty(changedProperty);
            Console.WriteLine($"{changedProperty} value changed to {property.GetValue(entity)} for charger {entity.Id} / {entity.Name}");

            // Run in background
            Task.Run(() =>
            {
                foreach (var subscriber in _subscribers.ToArray())
                {
                    subscriber.Invoke(entity, changedProperty);
                }
            }).ContinueWith(
                task =>
                {
                    Console.WriteLine("#### Error invoking message subscribers: " + task.Exception?.Message);
                    if (task.Exception != null)
                        Console.WriteLine(task.Exception?.StackTrace);
                },
                TaskContinuationOptions.OnlyOnFaulted
            );
        }
    }
}