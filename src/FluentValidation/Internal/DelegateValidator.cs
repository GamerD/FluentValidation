#region License
// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://www.codeplex.com/FluentValidation
#endregion

namespace FluentValidation.Internal {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Results;
	using Validators;
  using System.Linq.Expressions;

	/// <summary>
	/// Custom IValidationRule for performing custom logic.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DelegateValidator<T> : IValidationRule {
		private readonly Func<T, ValidationContext<T>, IEnumerable<ValidationFailure>> func;
		private readonly Func<T, ValidationContext<T>, Task<IEnumerable<ValidationFailure>>> asyncFunc;

		// Work-around for reflection bug in .NET 4.5
		static Func<object, bool> s_condition = x => true;
		private Func<object, bool> condition = s_condition;
    private Expression expression;
    public Expression Expression { get { return expression; } }

		/// <summary>
		/// Rule set to which this rule belongs.
		/// </summary>
		public string RuleSet { get; set; }

		/// <summary>
		/// Creates a new DelegateValidator using the specified function to perform validation.
		/// </summary>
		public DelegateValidator(Func<T, ValidationContext<T>, IEnumerable<ValidationFailure>> func) {
			this.func = func;
			asyncFunc = (x, ctx) => TaskHelpers.RunSynchronously(() => this.func(x, ctx));
		}

		/// <summary>
		/// Creates a new DelegateValidator using the specified function to perform validation.
		/// </summary>
		public DelegateValidator(Func<T, IEnumerable<ValidationFailure>> func)
			: this((x, ctx) => func(x)) {
		}

		/// <summary>
		/// Creates a new DelegateValidator using the specified async function to perform validation.
		/// </summary>
		public DelegateValidator(Func<T, ValidationContext<T>, Task<IEnumerable<ValidationFailure>>> asyncFunc) {
			this.asyncFunc = asyncFunc;
			func = (x, ctx) => this.asyncFunc(x, ctx).Result;
		}

		/// <summary>
		/// Creates a new DelegateValidator using the specified async function to perform validation.
		/// </summary>
		public DelegateValidator(Func<T, Task<IEnumerable<ValidationFailure>>> asyncFunc)
			: this((x, ctx) => asyncFunc(x)) {
		}

		/// <summary>
		/// Performs validation using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A collection of validation failures</returns>
		public IEnumerable<ValidationFailure> Validate(ValidationContext<T> context) {
			return func(context.InstanceToValidate, context);
		}

		/// <summary>
		/// Performs validation asynchronously using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A collection of validation failures</returns>
		public Task<IEnumerable<ValidationFailure>> ValidateAsync(ValidationContext<T> context) {
			return asyncFunc(context.InstanceToValidate, context);
		}

		/// <summary>
		/// The validators that are grouped under this rule.
		/// </summary>
		public IEnumerable<IPropertyValidator> Validators {
			get { yield break; }
		}

		/// <summary>
		/// Performs validation using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A collection of validation failures</returns>
		public IEnumerable<ValidationFailure> Validate(ValidationContext context) {
			if (!context.Selector.CanExecute(this, "", context) || !condition(context.InstanceToValidate)) {
				return Enumerable.Empty<ValidationFailure>();
			}

			var newContext = new ValidationContext<T>((T) context.InstanceToValidate, context.PropertyChain, context.Selector);
			return Validate(newContext);
		}

		/// <summary>
		/// When overloaded performs validation asynchronously using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A collection of validation failures</returns>
		public Task<IEnumerable<ValidationFailure>> ValidateAsync(ValidationContext context) {
			if (!context.Selector.CanExecute(this, "", context) || !condition(context.InstanceToValidate)) {
				return TaskHelpers.FromResult(Enumerable.Empty<ValidationFailure>());
			}

			var newContext = new ValidationContext<T>((T) context.InstanceToValidate, context.PropertyChain, context.Selector);
			return ValidateAsync(newContext);
		}

		public void ApplyCondition(Expression<Func<object, bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators) {
			// For custom rules within the DelegateValidator, we ignore ApplyConiditionTo - this is only relevant to chained rules using RuleFor.
			var originalCondition = this.condition;
			this.condition = x => predicate.Compile()(x) && originalCondition(x);
      this.expression = predicate;
		}
	}
}