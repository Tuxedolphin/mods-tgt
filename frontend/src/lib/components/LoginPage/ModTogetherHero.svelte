<script lang="ts">
	let { children } = $props();

	import mods_tgt_logo from '$lib/assets/mods_tgt_logo.png?enhanced';
</script>

<div class="hero min-h-screen bg-base-200">
	<div class="hero-content text-center">
		<div class="max-w-md">
			<enhanced:img class="align-middle" src={mods_tgt_logo} alt="Mods Together Logo" />
			<h1 class="text-3xl font-bold">Mods Together!</h1>
			<p class="py-4">We ride together. We mods together.</p>
			<div class="divider"></div>
			{@render children()}
		</div>
	</div>
</div>
