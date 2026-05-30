<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { registered } from '$lib/shared/shared.svelte';
	import RegisterButton from './RegisterButton.svelte';

	let emailInput = $state('');
	let passwordInput = $state('');
</script>

<div>Register!</div>
{#if !$registered}
	<form class="fieldset content-around px-4">
		<fieldset class="fieldset">
			<label class="label">Email</label>
			<input
				type="email"
				class="validator input w-full"
				placeholder="Email"
				required
				bind:value={emailInput}
			/>
			<p class="validator-hint hidden">Required</p>
		</fieldset>

		<label class="fieldset">
			<span class="label">Password</span>
			<input
				type="password"
				class="validator input w-full"
				placeholder="Password"
				required
				bind:value={passwordInput}
			/>
			<span class="validator-hint hidden">Required</span>
		</label>

		<RegisterButton email={emailInput} password={passwordInput}></RegisterButton>
	</form>
{:else}
	<div>
		Registration successful. A confirmation will be sent if it has not been used before. Check spam
		if necessary.
	</div>
	<button class="btn mt-4" type="submit" onclick={() => goto(resolve('/'))}>Back to Login</button>
{/if}
