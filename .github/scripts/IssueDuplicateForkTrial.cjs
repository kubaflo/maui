const crypto = require('node:crypto');
const { gather, validate } = require('./IssueDuplicates.cjs');

const publicationIssueNumber = 945;
const publicationBodyHash = 'adfdb414668cc1afa2fe3287a905855ee51e85909ca8f0c47ff99ef6a168bdbd';

async function assertPublicationTarget(github, context) {
    const { data: repository } = await github.rest.repos.get(context.repo);
    if (
        repository.full_name !== 'kubaflo/maui' ||
        repository.private !== false ||
        repository.visibility !== 'public'
    ) {
        throw new Error('Fork publication requires public kubaflo/maui.');
    }
    const { data: issue } = await github.rest.issues.get({
        ...context.repo,
        issue_number: publicationIssueNumber,
    });
    const bodyHash = crypto
        .createHash('sha256')
        .update(issue.body ?? '')
        .digest('hex');
    if (
        issue.id !== 5781031909 ||
        issue.number !== publicationIssueNumber ||
        issue.pull_request ||
        issue.user?.login !== 'kubaflo' ||
        issue.state !== 'open' ||
        issue.locked ||
        issue.comments !== 0 ||
        issue.title !==
            '[Duplicate detector publication trial] Android WebView/FlyoutPage RenderThread SIGSEGV' ||
        bodyHash !== publicationBodyHash ||
        issue.labels.some((label) => /^s\/duplicate\b/.test(label.name))
    ) {
        throw new Error('The fixed fork publication target changed or is no longer eligible.');
    }
}

async function getSourceContext(github, context, issueNumber) {
    if (
        context.repo.owner !== 'kubaflo' ||
        context.repo.repo !== 'maui' ||
        context.payload.repository.full_name !== 'kubaflo/maui' ||
        context.payload.repository.private !== false ||
        context.actor !== 'kubaflo' ||
        context.ref !== 'refs/heads/duplicate-detector-fork-trial-20261008' ||
        context.eventName !== 'workflow_dispatch' ||
        !['true', 'false', true, false].includes(context.payload.inputs?.staged) ||
        issueNumber !== 39220 ||
        (context.payload.inputs?.aw_context ?? '') !== ''
    ) {
        throw new Error('Untrusted fork trial context.');
    }
    const repository = { owner: 'dotnet', repo: 'maui' };
    const { data } = await github.rest.repos.get(repository);
    if (
        data.full_name !== 'dotnet/maui' ||
        data.private !== false ||
        data.visibility !== 'public'
    ) {
        throw new Error('Fork trial evidence must come from public dotnet/maui.');
    }
    // Only the evidence context is rebound; execution remains on the checked fork/ref.
    return {
        ...context,
        repo: repository,
        ref: `refs/heads/${data.default_branch}`,
        payload: { ...context.payload, repository: data },
    };
}

module.exports = {
    async gather(options) {
        const context = await getSourceContext(
            options.github,
            options.context,
            options.issueNumber,
        );
        await assertPublicationTarget(options.github, options.context);
        await gather({ ...options, context });
    },
    async validate(options) {
        const requestedStaging = ['true', true].includes(options.context.payload.inputs?.staged);
        if (options.staged !== requestedStaging) {
            throw new Error('The staging flag does not match the trusted dispatch.');
        }
        const context = await getSourceContext(
            options.github,
            options.context,
            options.issueNumber,
        );
        await assertPublicationTarget(options.github, options.context);
        await validate({
            ...options,
            context,
            reportRepository: options.context.repo,
            commentIssueNumber: publicationIssueNumber,
        });
        await assertPublicationTarget(options.github, options.context);
    },
};
