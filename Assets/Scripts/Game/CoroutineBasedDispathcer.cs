using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Game {
	public class CoroutineBasedDispathcer : MonoBehaviour {
		public void RegistrateTask<T> (System.Func<T> function, System.Action<T> callback) {
			var curTask = Task.Run(function);
			StartCoroutine(TaskWaiter<T>(curTask, callback));
		}

		private IEnumerator TaskWaiter<T> (Task<T> task, System.Action<T> callback) {
			while (!task.IsCompleted) {
				yield return null;
			}

			callback(task.Result);
		}
	}
}

