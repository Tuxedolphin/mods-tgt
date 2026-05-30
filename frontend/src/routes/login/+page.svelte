<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import LoginComponent from '$lib/components/LoginPage/LoginComponent.svelte';
	import ModTogetherHero from '$lib/components/LoginPage/ModTogetherHero.svelte';
	import { currentUserInformation } from '$lib/shared/shared.svelte';

	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});
</script>

<ModTogetherHero>
	<LoginComponent></LoginComponent>
	<!-- <div class="divider">OR</div> -->
	<!-- <GuestTryoutComponent></GuestTryoutComponent> -->
</ModTogetherHero>
