namespace SharedHolograms
{
    using UnityEngine;
    using System;

#if !UNITY_EDITOR
    using System.Threading.Tasks;

    public class UpdateCheckPredicate : MonoBehaviour
    {
        public UpdateCheckPredicate()
        {
            this.completed = new TaskCompletionSource<bool>();
        }
        public Func<bool> Predicate { get; set; }

        void Update()
        {

            if ((this.Predicate != null) && this.Predicate())
            {
                this.completed.SetResult(true);
            }
        }
        public async static Task WaitForPredicateAsync(
          GameObject gameObject,
          Func<bool> predicate)
        {
            var component = gameObject.AddComponent<UpdateCheckPredicate>();
            component.Predicate = predicate;
            await component.completed.Task;
            Destroy(component);
        }
        TaskCompletionSource<bool> completed;
    }
#endif
}