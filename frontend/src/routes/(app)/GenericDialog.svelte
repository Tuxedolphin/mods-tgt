<script lang="ts">
	let { dialog = $bindable<HTMLDialogElement>(), closeHandler, children } = $props();
</script>

<dialog bind:this={dialog} class="modal">
	<div class="modal-box">
		{@render children()}
	</div>
	<form method="dialog" class="modal-backdrop">
		<button
			onclick={() => {
				if (closeHandler) {
					closeHandler();
				}
			}}>close</button
		>
	</form>
</dialog>
