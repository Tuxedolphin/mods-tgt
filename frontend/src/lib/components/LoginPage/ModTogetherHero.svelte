<script lang="ts">
	let { children } = $props();
</script>

<div class="hero min-h-screen bg-base-200">
	<div class="hero-content text-center">
		<div class="max-w-md">
			<h1 class="text-5xl font-bold">Mods Together!</h1>
			<p class="py-4">We ride together. We mods together.</p>
			<div class="divider"></div>
			{@render children()}
		</div>
	</div>
</div>
