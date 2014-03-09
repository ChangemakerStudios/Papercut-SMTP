/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///     Provides functions used for code contracts.
    /// </summary>
    public static class CodeContracts
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Validates argument (obj) is not <see langword="null" />. Throws exception
        ///     if it is.
        /// </summary>
        /// <typeparam name="T">
        ///     type of the argument that's being verified
        /// </typeparam>
        /// <param name="obj">value of argument to verify not null</param>
        /// <param name="argumentName">name of the argument</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="obj" /> is
        ///     <c>null</c>.
        /// </exception>
        [ContractAnnotation("obj:null => halt")]
        public static void VerifyNotNull<T>([CanBeNull] T obj, string argumentName, [CallerMemberName] string callerMemberName = null) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(argumentName, string.Format("{0} cannot be null. CallerMemberName: {1}", argumentName, callerMemberName));
            }
        }

        #endregion
    }
}